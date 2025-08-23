using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering.Common;
using ParallelAnimationSystem.Rendering.OpenGL.PostProcessing;
using ParallelAnimationSystem.Rendering.TextProcessing;
using ParallelAnimationSystem.Util;
using ParallelAnimationSystem.Windowing;
using TmpIO;
using TmpParser;

namespace ParallelAnimationSystem.Rendering.OpenGL;

public class Renderer(IAppSettings appSettings, IWindowManager windowManager, IResourceManager resourceManager, ILogger<Renderer> logger) : IRenderer
{
    private record MeshHandle(int VertexOffset, int VertexCount, int IndexOffset, int IndexCount) : IMeshHandle;
    private class FontHandle(TmpFile fontFile, Dictionary<char, TmpCharacter> ordinalToCharacter, Dictionary<int, TmpGlyph> glyphIdToGlyph) : IFontHandle
    { 
        public TmpFile FontFile { get; } = fontFile;
        public TmpMetadata Metadata { get; } = fontFile.Metadata;
        public Dictionary<char, TmpCharacter> OrdinalToCharacter { get; } = ordinalToCharacter;
        public Dictionary<int, TmpGlyph> GlyphIdToGlyph { get; } = glyphIdToGlyph;
        public bool Initialized { get; private set; }
        public int AtlasHandle { get; private set; }

        public void Initialize(int atlasHandle)
        {
            if (Initialized)
                return;
            Initialized = true;
            
            AtlasHandle = atlasHandle;
        }
    }
    private record TextHandle(RenderGlyph[] Glyphs) : ITextHandle;
    
    private const int MaxFonts = 12;
    private const int MsaaSamples = 4;

    public IWindow Window => windowHandle ?? throw new InvalidOperationException("Renderer has not been initialized");
    public int QueuedDrawListCount => drawListQueue.Count;

    private IWindow? windowHandle;

    // Synchronization
    private readonly object meshDataLock = new();
    
    // Draw list stuff
    private readonly ConcurrentQueue<DrawList> drawListPool = [];
    private readonly ConcurrentQueue<DrawList> drawListQueue = [];
    
    // Rendering data
    private readonly Buffer vertexBuffer = new();
    private readonly Buffer indexBuffer = new();
    private bool newMeshData = true;
    
    // Post processors
    private readonly Bloom bloom = new(resourceManager);
    private readonly UberPost uberPost = new(resourceManager);
    
    // Graphics data
    private readonly List<FontHandle> registeredFonts = [];
    private IMeshHandle? baseFontMeshHandle;
    
    private int vertexArrayHandle, vertexBufferHandle, indexBufferHandle;
    private int multiDrawIndirectBufferHandle, multiDrawIndirectBufferSize;
    private int multiDrawStorageBufferHandle, multiDrawStorageBufferSize;
    private int multiDrawGlyphBufferHandle, multiDrawGlyphBufferSize;
    private int programHandle;
    private int fontAtlasesUniformLocation;
    private int fontAtlasSampler;
    
    private Vector2i currentFboSize;
    private int fboColorBufferHandle, fboDepthBufferHandle;
    private int fboHandle;
    private int postProcessTextureHandle1, postProcessTextureHandle2;
    private int postProcessFboHandle;

    private readonly List<DrawData> opaqueDrawData = [];
    private readonly List<DrawData> transparentDrawData = [];
    
    private readonly Buffer multiDrawIndirectBuffer = new();
    private readonly Buffer multiDrawStorageBuffer = new();
    private readonly Buffer multiDrawGlyphBuffer = new();
    
    public IMeshHandle RegisterMesh(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<int> indices)
    {
        lock (meshDataLock)
        {
            newMeshData = true;

            var vertexOffset = vertexBuffer.Data.Length / Vector2.SizeInBytes;
            var indexOffset = indexBuffer.Data.Length / sizeof(int);
            var vertexCount = vertices.Length;
            var indexCount = indices.Length;
            
            vertexBuffer.Append(vertices);
            indexBuffer.Append(indices);
            
            logger.LogInformation("Registered mesh with {VertexCount} vertices and {IndexCount} indices", vertexCount, indexCount);
            
            return new MeshHandle(vertexOffset, vertexCount, indexOffset, indexCount);
        }
    }

    public IFontHandle RegisterFont(Stream stream)
    {
        var fontFile = TmpRead.Read(stream);
        lock (registeredFonts)
        {
            var fontHandle = new FontHandle(
                fontFile,
                fontFile.Characters.ToDictionary(x => x.Character),
                fontFile.Glyphs.ToDictionary(x => x.Id));
            registeredFonts.Add(fontHandle);
            logger.LogInformation("Registered font '{FontName}'", fontFile.Metadata.Name);
            return fontHandle;
        }
    }

    public ITextHandle CreateText(string str, IReadOnlyList<FontStack> fontStacks, string defaultFontName, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment)
    {
        lock (registeredFonts)
        {
            var textShaper = new TextShaper<RenderGlyph>(
                (min, max, minUV, maxUV, color, boldItalic, fontIndex) 
                    => new RenderGlyph(min, max, minUV, maxUV, color, boldItalic, fontIndex),
                (x, c) =>
                {
                    var fontHandle = (FontHandle)x;
                    if (fontHandle.OrdinalToCharacter.TryGetValue(c, out var character))
                        return character;
                    return null;
                },
                (x, glyphId) =>
                {
                    var fontHandle = (FontHandle)x;
                    if (fontHandle.GlyphIdToGlyph.TryGetValue(glyphId, out var glyph))
                        return glyph;
                    return null;
                },
                x => ((FontHandle)x).Metadata,
                registeredFonts,
                fontStacks,
                defaultFontName);
            var shapedText = textShaper.ShapeText(str, horizontalAlignment, verticalAlignment);
            return new TextHandle(shapedText.ToArray());
        }
    }

    public void Initialize()
    {
        logger.LogInformation("Initializing renderer");

        // Create window
        windowHandle = windowManager.CreateWindow(
            "Parallel Animation System", 
            appSettings.InitialSize,
            new GLContextSettings
            {
                Version = new Version(4, 6),
                ES = false,
            });
        windowHandle.MakeContextCurrent();
        
        // Set swap interval
        windowHandle.SetSwapInterval(appSettings.SwapInterval);
        
        // Load OpenGL bindings
        GLLoader.LoadBindings(new BindingsContext(windowManager));
        
        logger.LogInformation("Window created");
        
        // Register text mesh
        baseFontMeshHandle = RegisterMesh([
            new Vector2(0.0f, 1.0f),
            new Vector2(1.0f, 1.0f),
            new Vector2(0.0f, 0.0f),
            new Vector2(1.0f, 0.0f),
        ], [
            0, 1, 2,
            3, 2, 1,
        ]);
        
        // Initialize OpenGL data
        InitializeOpenGlData(windowHandle.FramebufferSize);
        
        // Enable multisampling
        GL.Enable(EnableCap.Multisample);
        
        // Log OpenGL info
        logger.LogInformation("OpenGL: {Version}", GL.GetString(StringName.Version));
        logger.LogInformation("Renderer: {Renderer}", GL.GetString(StringName.Renderer));
        logger.LogInformation("Vendor: {Vendor}", GL.GetString(StringName.Vendor));
        logger.LogInformation("Shading language: {ShadingLanguage}", GL.GetString(StringName.ShadingLanguageVersion));
    }

    public IDrawList GetDrawList()
    {
        if (!drawListPool.TryDequeue(out var drawList))
            drawList = new DrawList();
        return drawList;
    }

    public void SubmitDrawList(IDrawList drawList)
    {
        drawListQueue.Enqueue((DrawList) drawList);
    }

    private void InitializeOpenGlData(Vector2i size)
    {
        // We will exclusively use DSA for this project
        vertexArrayHandle = GL.CreateVertexArray();
        vertexBufferHandle = GL.CreateBuffer();
        indexBufferHandle = GL.CreateBuffer();

        // Upload data to GPU
        lock (meshDataLock)
        {
            var vertexBufferData = vertexBuffer.Data;
            var indexBufferData = indexBuffer.Data;
            var vertexBufferSize = vertexBufferData.Length * Vector2.SizeInBytes;
            var indexBufferSize = indexBufferData.Length * sizeof(int);
        
            GL.NamedBufferData(vertexBufferHandle, vertexBufferSize, vertexBuffer.Data, VertexBufferObjectUsage.StaticDraw);
            GL.NamedBufferData(indexBufferHandle, indexBufferSize, indexBuffer.Data, VertexBufferObjectUsage.StaticDraw);
        
            newMeshData = false;
        }
        
        // Bind buffers to vertex array
        GL.EnableVertexArrayAttrib(vertexArrayHandle, 0);
        GL.VertexArrayVertexBuffer(vertexArrayHandle, 0, vertexBufferHandle, IntPtr.Zero, Vector2.SizeInBytes);
        GL.VertexArrayAttribFormat(vertexArrayHandle, 0, 2, VertexAttribType.Float, false, 0);
        GL.VertexArrayAttribBinding(vertexArrayHandle, 0, 0);
                
        GL.VertexArrayElementBuffer(vertexArrayHandle, indexBufferHandle);
        
        // Initialize multi draw buffer
        multiDrawIndirectBufferHandle = GL.CreateBuffer();
        multiDrawStorageBufferHandle = GL.CreateBuffer();
        multiDrawGlyphBufferHandle = GL.CreateBuffer();
        
        // Initialize shader program
        var vertexShaderSource = resourceManager.LoadResourceString("OpenGL/Shaders/UberVertex.glsl");
        var fragmentShaderSource = resourceManager.LoadResourceString("OpenGL/Shaders/UberFragment.glsl");
        
        // Create shaders
        var vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);
        
        // Check for vertex shader compilation errors
        var vertexStatus = GL.GetShaderi(vertexShader, ShaderParameterName.CompileStatus);
        if (vertexStatus == 0)
        {
            GL.GetShaderInfoLog(vertexShader, out var infoLog);
            throw new InvalidOperationException($"Vertex shader compilation failed: {infoLog}");
        }
        
        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);
        
        // Check for fragment shader compilation errors
        var fragmentStatus = GL.GetShaderi(fragmentShader, ShaderParameterName.CompileStatus);
        if (fragmentStatus == 0)
        {
            GL.GetShaderInfoLog(fragmentShader, out var infoLog);
            throw new InvalidOperationException($"Fragment shader compilation failed: {infoLog}");
        }
        
        // Create program
        programHandle = GL.CreateProgram();
        GL.AttachShader(programHandle, vertexShader);
        GL.AttachShader(programHandle, fragmentShader);
        GL.LinkProgram(programHandle);
        
        // Check for program linking errors
        var linkStatus = GL.GetProgrami(programHandle, ProgramProperty.LinkStatus);
        if (linkStatus == 0)
        {
            GL.GetProgramInfoLog(programHandle, out var infoLog);
            throw new InvalidOperationException($"Program linking failed: {infoLog}");
        }
        
        // Clean up
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
        
        // Get uniform locations
        fontAtlasesUniformLocation = GL.GetUniformLocation(programHandle, "uFontAtlases");
        
        // Initialize font atlas sampler
        fontAtlasSampler = GL.CreateSampler();
        GL.SamplerParameteri(fontAtlasSampler, SamplerParameterI.TextureMinFilter, (int) TextureMinFilter.Linear);
        GL.SamplerParameteri(fontAtlasSampler, SamplerParameterI.TextureMagFilter, (int) TextureMagFilter.Linear);
        
        // Initialize fbos
        // Initialize scene fbo
        fboColorBufferHandle = GL.CreateRenderbuffer();
        GL.NamedRenderbufferStorageMultisample(fboColorBufferHandle, MsaaSamples, InternalFormat.Rgba16f, size.X, size.Y);
        
        fboDepthBufferHandle = GL.CreateRenderbuffer();
        GL.NamedRenderbufferStorageMultisample(fboDepthBufferHandle, MsaaSamples, InternalFormat.DepthComponent32f, size.X, size.Y);
        
        fboHandle = GL.CreateFramebuffer();
        GL.NamedFramebufferRenderbuffer(fboHandle, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, fboColorBufferHandle);
        GL.NamedFramebufferRenderbuffer(fboHandle, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, fboDepthBufferHandle);
        
        // Initialize post process fbo
        postProcessTextureHandle1 = GL.CreateTexture(TextureTarget.Texture2d);
        GL.TextureStorage2D(postProcessTextureHandle1, 1, SizedInternalFormat.Rgba16f, size.X, size.Y);
        
        postProcessTextureHandle2 = GL.CreateTexture(TextureTarget.Texture2d);
        GL.TextureStorage2D(postProcessTextureHandle2, 1, SizedInternalFormat.Rgba16f, size.X, size.Y);
        
        postProcessFboHandle = GL.CreateFramebuffer();
        // We will bind the texture later
        
        currentFboSize = size;
        
        // Initialize post processors
        uberPost.Initialize();
        bloom.Initialize();
    }

    public bool ProcessFrame()
    {
        if (windowHandle is null)
            return false;
        
        // Check if window is closed
        if (windowHandle.ShouldClose)
            return false;
        
        // Get current draw list
        if (!drawListQueue.TryDequeue(out var drawList))
            return false;
        
        // Request animation frame
        windowHandle.RequestAnimationFrame((_, drawFramebuffer) =>
        {
            // Check framebuffer size
            var size = windowHandle.FramebufferSize;
            if (size.X <= 0 || size.Y <= 0)
                return false;
            
            RenderFrame(size, drawList, drawFramebuffer);
            return true;
        });
        return true;
    }
    
    private void RenderFrame(Vector2i size, DrawList drawList, int drawFramebuffer)
    {
        var renderWidth = size.X;
        var renderHeight = size.Y;
        if (appSettings.AspectRatio.HasValue)
        {
            var targetAspectRatio = appSettings.AspectRatio.Value;
            var screenAspectRatio = size.X / (float) size.Y;
            if (targetAspectRatio < screenAspectRatio)
            {
                renderWidth = (int) (size.Y * targetAspectRatio);
            }
            else
            {
                renderHeight = (int) (size.X / targetAspectRatio);
            }
        }
        var renderSize = new Vector2i(renderWidth, renderHeight);
        
        // Update OpenGL data
        UpdateOpenGlData(renderSize);
        
        // Split draw list into opaque and transparent
        opaqueDrawData.Clear();
        transparentDrawData.Clear();
        
        foreach (var drawData in drawList)
        {
            // Discard draw data that is fully transparent
            if (drawData.Color1.W == 0.0f && drawData.Color2.W == 0.0f)
                continue;
            
            // Add to appropriate list
            if (drawData.RenderType != RenderType.Text && drawData.Color1.W == 1.0f && (drawData.Color2.W == 1.0f || drawData.RenderMode == RenderMode.Normal))
                opaqueDrawData.Add(drawData);
            else
                transparentDrawData.Add(drawData);
        }
        
        // Reverse opaque draw data list so that it is drawn
        // from back to front to avoid overdraw
        opaqueDrawData.Reverse();
        
        // Get camera matrix (view and projection)
        var camera = GetCameraMatrix(drawList.CameraData, renderSize);
        
        // Render
        GL.Viewport(0, 0, currentFboSize.X, currentFboSize.Y);
        
        // Clear buffers
        var clearColor = drawList.ClearColor;
        var depth = 0.0f;
        GL.ClearNamedFramebufferf(fboHandle, OpenTK.Graphics.OpenGL.Buffer.Color, 0, in clearColor.X);
        GL.ClearNamedFramebufferf(fboHandle, OpenTK.Graphics.OpenGL.Buffer.Depth, 0, in depth);
        
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboHandle);
        
        // Use our program
        GL.UseProgram(programHandle);
        
        // Bind atlas texture
        lock (registeredFonts)
        {
            if (registeredFonts.Count > MaxFonts)
                throw new NotSupportedException($"The system has a limit of {MaxFonts} fonts, consider raising the limit");

            for (var i = 0; i < registeredFonts.Count; i++)
            {
                var fontData = registeredFonts[i];
                GL.Uniform1i(fontAtlasesUniformLocation + i, 1, i);
                GL.BindTextureUnit((uint) i, fontData.AtlasHandle);
                GL.BindSampler((uint) i, fontAtlasSampler);
            }
        }
        
        // Bind indirect buffer
        GL.BindBuffer(BufferTarget.DrawIndirectBuffer, multiDrawIndirectBufferHandle);
        
        // Bind storage buffer
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, multiDrawStorageBufferHandle);
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, multiDrawGlyphBufferHandle);
        
        // Bind our vertex array
        GL.BindVertexArray(vertexArrayHandle);
        
        // Set depth function
        GL.DepthFunc(DepthFunction.Greater);
        
        // Opaque pass, disable blending, enable depth testing
        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.DepthTest);
        
        // Render opaque draw data
        RenderDrawDataList(opaqueDrawData, camera);
        
        // Transparent pass, enable blending, disable depth write
        GL.BlendFuncSeparate(
            BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha,
            BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
        GL.Enable(EnableCap.Blend);
        GL.DepthMask(false);
        
        // Render transparent draw data
        RenderDrawDataList(transparentDrawData, camera);
        
        // Restore depth write state
        GL.DepthMask(true);
        
        // Unbind fbo
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        
        // Blit to post process fbo
        GL.NamedFramebufferTexture(postProcessFboHandle, FramebufferAttachment.ColorAttachment0, postProcessTextureHandle1, 0);
        
        GL.BlitNamedFramebuffer(
            fboHandle, postProcessFboHandle, 
            0, 0,
            renderSize.X, renderSize.Y,
            0, 0,
            renderSize.X, renderSize.Y,
            ClearBufferMask.ColorBufferBit,
            BlitFramebufferFilter.Linear);
        
        // Do post-processing
        var finalTexture = HandlePostProcessing(drawList.PostProcessingData, postProcessTextureHandle1, postProcessTextureHandle2);
        
        // Bind final texture to post process fbo
        GL.NamedFramebufferTexture(postProcessFboHandle, FramebufferAttachment.ColorAttachment0, finalTexture, 0);
        
        // Clear window
        var windowClearColor = Vector4.Zero;
        GL.ClearNamedFramebufferf(drawFramebuffer, OpenTK.Graphics.OpenGL.Buffer.Color, 0, in windowClearColor.X);
        
        // Blit to window
        var screenOffsetX = (size.X - renderSize.X) / 2;
        var screenOffsetY = (size.Y - renderSize.Y) / 2;
        
        GL.BlitNamedFramebuffer(
            postProcessFboHandle, drawFramebuffer,
            0, 0,
            renderSize.X, renderSize.Y,
            screenOffsetX, screenOffsetY,
            screenOffsetX + renderSize.X, screenOffsetY + renderSize.Y,
            ClearBufferMask.ColorBufferBit,
            BlitFramebufferFilter.Linear);
        
        // Return draw list to pool
        drawList.Reset();
        drawListPool.Enqueue(drawList);
    }

    private void RenderDrawDataList(List<DrawData> drawDataList, Matrix3 camera)
    {
        if (drawDataList.Count == 0)
            return;
        
        multiDrawIndirectBuffer.Clear();
        multiDrawStorageBuffer.Clear();
        multiDrawGlyphBuffer.Clear();
        
        var baseFontMesh = baseFontMeshHandle as MeshHandle;
        
        // Append data
        foreach (var drawData in drawDataList)
        {
            var renderType = drawData.RenderType;
            var transform = drawData.Transform;
            var color1 = drawData.Color1;
            var color2 = drawData.Color2;
            var renderMode = drawData.RenderMode;
            
            var mvp = transform * camera;
            
            multiDrawStorageBuffer.Append(new MultiDrawItem
            {
                MvpRow1 = mvp.Row0,
                MvpRow2 = mvp.Row1,
                MvpRow3 = mvp.Row2,
                Color1 = (Vector4) color1,
                Color2 = (Vector4) color2,
                Z = RenderUtil.EncodeIntDepth(drawData.Index),
                RenderMode = (int) renderMode,
                RenderType = (int) renderType,
                GlyphOffset = multiDrawGlyphBuffer.Data.Length / Unsafe.SizeOf<RenderGlyph>(),
            });

            switch (renderType)
            {
                case RenderType.Mesh when drawData.Mesh is MeshHandle mesh:
                    multiDrawIndirectBuffer.Append(new DrawElementsIndirectCommand
                    {
                        Count = mesh.IndexCount,
                        InstanceCount = 1,
                        FirstIndex = mesh.IndexOffset,
                        BaseVertex = mesh.VertexOffset,
                        BaseInstance = 0
                    });
                    break;
                case RenderType.Text when drawData.Text is TextHandle text:
                    Debug.Assert(baseFontMesh is not null);
                    multiDrawIndirectBuffer.Append(new DrawElementsIndirectCommand
                    {
                        Count = baseFontMesh.IndexCount,
                        InstanceCount = text.Glyphs.Length,
                        FirstIndex = baseFontMesh.IndexOffset,
                        BaseVertex = baseFontMesh.VertexOffset,
                        BaseInstance = 0
                    });
                    multiDrawGlyphBuffer.Append<RenderGlyph>(text.Glyphs);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported or invalid data for render type '{renderType}'");
            }
        }
        
        // Upload buffers to GPU
        var multiDrawIndirectBufferData = multiDrawIndirectBuffer.Data;
        var multiDrawStorageBufferData = multiDrawStorageBuffer.Data;
        var multiDrawGlyphBufferData = multiDrawGlyphBuffer.Data;
        
        if (multiDrawIndirectBufferData.Length > 0)
        {
            if (multiDrawIndirectBufferData.Length > multiDrawIndirectBufferSize)
            {
                multiDrawIndirectBufferSize = multiDrawIndirectBufferData.Length;
                GL.NamedBufferData(multiDrawIndirectBufferHandle, multiDrawIndirectBufferData.Length, multiDrawIndirectBufferData, VertexBufferObjectUsage.DynamicDraw);
            }
            else
                GL.NamedBufferSubData(multiDrawIndirectBufferHandle, IntPtr.Zero, multiDrawIndirectBufferData.Length, multiDrawIndirectBufferData);
        }

        if (multiDrawStorageBufferData.Length > 0)
        {
            if (multiDrawStorageBufferData.Length > multiDrawStorageBufferSize)
            {
                multiDrawStorageBufferSize = multiDrawStorageBufferData.Length;
                GL.NamedBufferData(multiDrawStorageBufferHandle, multiDrawStorageBufferData.Length, multiDrawStorageBufferData, VertexBufferObjectUsage.DynamicDraw);
            }
            else
                GL.NamedBufferSubData(multiDrawStorageBufferHandle, IntPtr.Zero, multiDrawStorageBufferData.Length, multiDrawStorageBufferData);
        }

        if (multiDrawGlyphBufferData.Length > 0)
        {
            if (multiDrawGlyphBufferData.Length > multiDrawGlyphBufferSize)
            {
                multiDrawGlyphBufferSize = multiDrawGlyphBufferData.Length;
                GL.NamedBufferData(multiDrawGlyphBufferHandle, multiDrawGlyphBufferData.Length, multiDrawGlyphBufferData, VertexBufferObjectUsage.DynamicDraw);
            }
            else
                GL.NamedBufferSubData(multiDrawGlyphBufferHandle, IntPtr.Zero, multiDrawGlyphBufferData.Length, multiDrawGlyphBufferData);
        }

        // Draw
        GL.MultiDrawElementsIndirect(
            PrimitiveType.Triangles,
            DrawElementsType.UnsignedInt,
            IntPtr.Zero,
            drawDataList.Count,
            0);
    }

    private int HandlePostProcessing(PostProcessingData data, int texture1, int texture2)
    {
        if (bloom.Process(currentFboSize, data.BloomIntensity, data.BloomDiffusion, texture1, texture2))
            Swap(ref texture1, ref texture2);
        
        if (uberPost.Process(
                currentFboSize,
                data.Time,
                data.HueShiftAngle,
                data.LensDistortionIntensity, data.LensDistortionCenter,
                data.ChromaticAberrationIntensity,
                data.VignetteCenter, data.VignetteIntensity, data.VignetteRounded, data.VignetteRoundness, data.VignetteSmoothness, data.VignetteColor,
                data.GradientColor1, data.GradientColor2, data.GradientIntensity, data.GradientRotation, data.GradientMode,
                data.GlitchIntensity, data.GlitchSpeed, data.GlitchSize,
                texture1, texture2))
            Swap(ref texture1, ref texture2);
        
        return texture1;
    }

    private static Matrix3 GetCameraMatrix(CameraData camera, Vector2i size)
    {
        var aspectRatio = size.X / (float) size.Y;
        var view = Matrix3.Invert(
            MathUtil.CreateScale(Vector2.One * camera.Scale) *
            MathUtil.CreateRotation(camera.Rotation) *
            MathUtil.CreateTranslation(camera.Position));
        var projection = Matrix3.CreateScale(1.0f / aspectRatio, 1.0f, 1.0f);
        return view * projection;
    }

    private void UpdateOpenGlData(Vector2i size)
    {
        UpdateMeshData();
        UpdateFontData();
        UpdateFboData(size);
    }

    private void UpdateMeshData()
    {
        lock (meshDataLock)
        {
            if (!newMeshData)
                return;
            
            // Update our buffers with the new data
            newMeshData = false;
            
            var vertexBufferData = vertexBuffer.Data;
            var indexBufferData = indexBuffer.Data;
                
            GL.NamedBufferSubData(vertexBufferHandle, IntPtr.Zero, vertexBufferData.Length, vertexBufferData);
            GL.NamedBufferSubData(indexBufferHandle, IntPtr.Zero, indexBufferData.Length, indexBufferData);
        }
        
        logger.LogInformation("OpenGL mesh buffers updated, now at {VertexSize} vertices and {IndexSize} indices", vertexBuffer.Data.Length / Vector2.SizeInBytes, indexBuffer.Data.Length / sizeof(int));
    }
    
    private void UpdateFontData()
    {
        lock (registeredFonts)
        {
            foreach (var fontData in registeredFonts.Where(x => !x.Initialized))
            {
                var fontFile = fontData.FontFile;
                var atlas = fontFile.Atlas;
                
                // Create texture
                var atlasHandle = GL.CreateTexture(TextureTarget.Texture2d);
                GL.TextureStorage2D(atlasHandle, 1, SizedInternalFormat.Rgb32f, atlas.Width, atlas.Height);
                GL.TextureSubImage2D(atlasHandle, 0, 0, 0, atlas.Width, atlas.Height, PixelFormat.Rgb, PixelType.Float, atlas.Data);
                
                fontData.Initialize(atlasHandle);
                
                logger.LogInformation("Font '{FontName}' atlas texture created", fontData.FontFile.Metadata.Name);
            }
        }
    }

    private void UpdateFboData(Vector2i size)
    {
        if (size == currentFboSize)
            return;
        
        // Delete old textures
        GL.DeleteRenderbuffer(fboColorBufferHandle);
        GL.DeleteRenderbuffer(fboDepthBufferHandle);
        GL.DeleteTexture(postProcessTextureHandle1);
        GL.DeleteTexture(postProcessTextureHandle2);
        
        // Create new textures
        fboColorBufferHandle = GL.CreateRenderbuffer();
        GL.NamedRenderbufferStorageMultisample(fboColorBufferHandle, MsaaSamples, InternalFormat.Rgba16f, size.X, size.Y);
        
        fboDepthBufferHandle = GL.CreateRenderbuffer();
        GL.NamedRenderbufferStorageMultisample(fboDepthBufferHandle, MsaaSamples, InternalFormat.DepthComponent32f, size.X, size.Y);
        
        postProcessTextureHandle1 = GL.CreateTexture(TextureTarget.Texture2d);
        GL.TextureStorage2D(postProcessTextureHandle1, 1, SizedInternalFormat.Rgba16f, size.X, size.Y);
        
        postProcessTextureHandle2 = GL.CreateTexture(TextureTarget.Texture2d);
        GL.TextureStorage2D(postProcessTextureHandle2, 1, SizedInternalFormat.Rgba16f, size.X, size.Y);
        
        // Bind to fbo
        GL.NamedFramebufferRenderbuffer(fboHandle, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, fboColorBufferHandle);
        GL.NamedFramebufferRenderbuffer(fboHandle, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, fboDepthBufferHandle);

        currentFboSize = size;
        
        logger.LogInformation("OpenGL framebuffer size updated, now at {Width}x{Height}", currentFboSize.X, currentFboSize.Y);
    }

    public void Dispose()
    {
        if (windowHandle is IDisposable disposable)
            disposable.Dispose();
    }

    private static void Swap<T>(ref T a, ref T b)
    {
        (a, b) = (b, a);
    }
}