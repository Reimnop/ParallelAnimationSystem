using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGLES2;
using OpenTK.Mathematics;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering.Common;
using ParallelAnimationSystem.Rendering.OpenGLES.PostProcessing;
using ParallelAnimationSystem.Rendering.TextProcessing;
using ParallelAnimationSystem.Util;
using ParallelAnimationSystem.Windowing;
using TmpIO;
using TmpParser;

namespace ParallelAnimationSystem.Rendering.OpenGLES;

public class Renderer(IAppSettings appSettings, IWindowManager windowManager, IResourceManager resourceManager, ILogger<Renderer> logger) : IRenderer
{
    private class MeshHandle : IMeshHandle
    {
        public int VertexArrayHandle { get; set; }
        public int IndexBufferHandle { get; set; }
        public int Count { get; set; }
    }
    
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
    
    // Draw list stuff
    private readonly ConcurrentQueue<DrawList> drawListPool = [];
    private readonly ConcurrentQueue<DrawList> drawListQueue = [];
    
    // Graphics data
    private readonly ConcurrentQueue<Action> initializers = [];
    
    private readonly List<FontHandle> registeredFonts = [];
    
    private int programHandle;
    private int mvpUniformLocation, zUniformLocation, renderModeUniformLocation, color1UniformLocation, color2UniformLocation;
    private int glyphProgramHandle;
    private int glyphMvpUniformLocation, glyphZUniformLocation, glyphFontAtlasesUniformLocation, glyphBaseColorUniformLocation;

    private int textVertexArrayHandle, textInstanceBufferHandle, textBufferCurrentSize;
    
    private int fboColorBufferHandle, fboDepthBufferHandle;
    private int fboHandle;
    private int postProcessTextureHandle1, postProcessTextureHandle2;
    private int postProcessFboHandle;
    private Vector2i currentFboSize;
    
    // Post-processing
    private readonly Bloom bloom = new(resourceManager);
    private readonly UberPost uberPost = new(resourceManager);
    
    // Temporary draw data lists
    private readonly List<DrawData> opaqueDrawData = [];
    private readonly List<DrawData> transparentDrawData = [];
    
    private IWindow? windowHandle;
    
    public void Initialize()
    {
        logger.LogInformation("Initializing renderer");
        
        // Create window
        windowHandle = windowManager.CreateWindow(
            "Parallel Animation System", 
            appSettings.InitialSize, 
            new GLContextSettings
            {
                Version = new Version(3, 0),
                ES = true
            });
        windowHandle.MakeContextCurrent();
        
        // Set swap interval
        windowHandle.SetSwapInterval(appSettings.SwapInterval);
        
        // Load OpenGL bindings
        GLLoader.LoadBindings(new BindingsContext(windowManager));
        
        logger.LogInformation("Window created");
        
        // Initialize OpenGL data
        InitializeOpenGlData(windowHandle.FramebufferSize);
        
        // Log OpenGL information
        logger.LogInformation("OpenGL ES: {Version}", GL.GetString(StringName.Version));
        logger.LogInformation("Renderer: {Renderer}", GL.GetString(StringName.Renderer));
        logger.LogInformation("Vendor: {Vendor}", GL.GetString(StringName.Vendor));
        logger.LogInformation("Shading language: {ShadingLanguage}", GL.GetString(StringName.ShadingLanguageVersion));
    }

    private void InitializeOpenGlData(Vector2i initialSize)
    {
        // Create main program handle
        programHandle = CreateShaderProgram("Shaders/UnlitVertex.glsl", "Shaders/UnlitFragment.glsl");
        
        // Get uniform locations
        mvpUniformLocation = GL.GetUniformLocation(programHandle, "uMvp");
        zUniformLocation = GL.GetUniformLocation(programHandle, "uZ");
        renderModeUniformLocation = GL.GetUniformLocation(programHandle, "uRenderMode");
        color1UniformLocation = GL.GetUniformLocation(programHandle, "uColor1");
        color2UniformLocation = GL.GetUniformLocation(programHandle, "uColor2");
        
        // Create glyph program handle
        glyphProgramHandle = CreateShaderProgram("Shaders/TextVertex.glsl", "Shaders/TextFragment.glsl");
        
        // Get glyph uniform locations
        glyphMvpUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uMvp");
        glyphZUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uZ");
        glyphFontAtlasesUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uFontAtlases");
        glyphBaseColorUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uBaseColor");
        
        // Initialize text buffers
        var renderGlyphSize = Unsafe.SizeOf<RenderGlyph>();
        
        textInstanceBufferHandle = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, textInstanceBufferHandle);
        GL.BufferData(BufferTarget.ArrayBuffer, 1024 * renderGlyphSize, IntPtr.Zero, BufferUsage.DynamicDraw);
        textBufferCurrentSize = 1024 * renderGlyphSize;
        
        textVertexArrayHandle = GL.GenVertexArray();
        GL.BindVertexArray(textVertexArrayHandle);
        
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, renderGlyphSize, 0); // Min
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, renderGlyphSize, 8); // Max
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, renderGlyphSize, 16); // MinUV
        GL.EnableVertexAttribArray(3);
        GL.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, renderGlyphSize, 24); // MaxUV
        GL.EnableVertexAttribArray(4);
        GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, renderGlyphSize, 32); // Color
        GL.EnableVertexAttribArray(5);
        GL.VertexAttribIPointer(5, 1, VertexAttribIType.Int, renderGlyphSize, 48); // BoldItalic
        GL.EnableVertexAttribArray(6);
        GL.VertexAttribIPointer(6, 1, VertexAttribIType.Int, renderGlyphSize, 52); // FontIndex
        
        // Divisors
        GL.VertexAttribDivisor(0, 1);
        GL.VertexAttribDivisor(1, 1);
        GL.VertexAttribDivisor(2, 1);
        GL.VertexAttribDivisor(3, 1);
        GL.VertexAttribDivisor(4, 1);
        GL.VertexAttribDivisor(5, 1);
        GL.VertexAttribDivisor(6, 1);
        
        // Initialize FBO
        fboColorBufferHandle = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, fboColorBufferHandle);
        GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, MsaaSamples, InternalFormat.Rgba16f, initialSize.X, initialSize.Y);
        
        fboDepthBufferHandle = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, fboDepthBufferHandle);
        GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, MsaaSamples, InternalFormat.DepthComponent32f, initialSize.X, initialSize.Y);
        
        fboHandle = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboHandle);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, fboColorBufferHandle);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, fboDepthBufferHandle);
        
        // Check FBO status
        var fboStatus = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (fboStatus != FramebufferStatus.FramebufferComplete)
            throw new InvalidOperationException($"Framebuffer is not complete: {fboStatus}");
        
        // Initialize post-process FBO
        postProcessTextureHandle1 = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, postProcessTextureHandle1);
        GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgba16f, initialSize.X, initialSize.Y);
        
        postProcessTextureHandle2 = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, postProcessTextureHandle2);
        GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgba16f, initialSize.X, initialSize.Y);
        
        postProcessFboHandle = GL.GenFramebuffer();
        
        // Check post-process FBO status
        var postProcessFboStatus = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (postProcessFboStatus != FramebufferStatus.FramebufferComplete)
            throw new InvalidOperationException($"Post-process framebuffer is not complete: {postProcessFboStatus}");
        
        currentFboSize = initialSize;
        
        // Initialize post-processing
        var vertexShaderSource = resourceManager.LoadResourceString("OpenGLES/Shaders/PostProcessVertex.glsl");
        var vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);
        
        var vertexShaderCompileStatus = GL.GetShaderi(vertexShader, ShaderParameterName.CompileStatus);
        if (vertexShaderCompileStatus == 0)
        {
            GL.GetShaderInfoLog(vertexShader, out var infoLog);
            throw new InvalidOperationException($"Failed to compile vertex shader: {infoLog}");
        }
        
        uberPost.Initialize(vertexShader);
        bloom.Initialize(vertexShader);
        
        // Clean up
        GL.DeleteShader(vertexShader);
    }

    private int CreateShaderProgram(string vertexShaderResourceName, string fragmentShaderResourceName)
    {
        // Initialize shader program
        var vertexShaderSource = resourceManager.LoadResourceString($"OpenGLES/{vertexShaderResourceName}");
        var fragmentShaderSource = resourceManager.LoadResourceString($"OpenGLES/{fragmentShaderResourceName}");
        
        var vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);
        
        var vertexLinkStatus = GL.GetShaderi(vertexShader, ShaderParameterName.CompileStatus);
        if (vertexLinkStatus == 0)
        {
            GL.GetShaderInfoLog(vertexShader, out var infoLog);
            throw new InvalidOperationException($"Vertex shader compilation failed: {infoLog}");
        }
        
        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);
        
        var fragmentLinkStatus = GL.GetShaderi(fragmentShader, ShaderParameterName.CompileStatus);
        if (fragmentLinkStatus == 0)
        {
            GL.GetShaderInfoLog(fragmentShader, out var infoLog);
            throw new InvalidOperationException($"Fragment shader compilation failed: {infoLog}");
        }
        
        var programHandle = GL.CreateProgram();
        GL.AttachShader(programHandle, vertexShader);
        GL.AttachShader(programHandle, fragmentShader);
        GL.LinkProgram(programHandle);
        
        var linkStatus = GL.GetProgrami(programHandle, ProgramProperty.LinkStatus);
        if (linkStatus == 0)
        {
            GL.GetProgramInfoLog(programHandle, out var infoLog);
            throw new InvalidOperationException($"Program linking failed: {infoLog}");
        }
        
        // Clean up
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
        
        return programHandle;
    }

    public IMeshHandle RegisterMesh(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<int> indices)
    {
        var verticesBuffer = vertices.ToArray();
        var indicesBuffer = indices.ToArray();
        
        var meshHandle = new MeshHandle();
        var intializer = () =>
        {
            var vertexBufferHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferHandle);
        
            var indexBufferHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferHandle);
            
            var vertexBufferSize = verticesBuffer.Length * Vector2.SizeInBytes;
            var indexBufferSize = indicesBuffer.Length * sizeof(int);
        
            GL.BufferData(BufferTarget.ArrayBuffer, vertexBufferSize, verticesBuffer, BufferUsage.StaticDraw);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indexBufferSize, indicesBuffer, BufferUsage.StaticDraw);
        
            // Initialize VAO
            var vertexArrayHandle = GL.GenVertexArray();
            GL.BindVertexArray(vertexArrayHandle);
        
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Vector2.SizeInBytes, 0);
            
            // Set mesh handle data
            meshHandle.VertexArrayHandle = vertexArrayHandle;
            meshHandle.IndexBufferHandle = indexBufferHandle;
            meshHandle.Count = indicesBuffer.Length;
        };
        
        initializers.Enqueue(intializer);
        logger.LogInformation("Registered mesh with {VertexCount} vertices and {IndexCount} indices", vertices.Length, indices.Length);
        
        return meshHandle;
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

            var initializer = () =>
            {
                var atlasHandle = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2d, atlasHandle);
                GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgb32f, fontFile.Atlas.Width, fontFile.Atlas.Height);
                GL.TexSubImage2D(TextureTarget.Texture2d, 0, 0, 0, fontFile.Atlas.Width, fontFile.Atlas.Height, PixelFormat.Rgb, PixelType.Float, fontFile.Atlas.Data);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                
                fontHandle.Initialize(atlasHandle);
            };
            initializers.Enqueue(initializer);
            
            logger.LogInformation("Registered font '{FontName}'", fontFile.Metadata.Name);
            return fontHandle;
        }
    }

    public ITextHandle CreateText(
        string str,
        IReadOnlyList<FontStack> fontStacks,
        string defaultFontName,
        HorizontalAlignment horizontalAlignment,
        VerticalAlignment verticalAlignment)
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

    public IDrawList GetDrawList()
    {
        if (drawListPool.TryDequeue(out var drawList))
            return drawList;
        drawList = new DrawList();
        return drawList;
    }

    public void SubmitDrawList(IDrawList drawList)
    {
        drawListQueue.Enqueue((DrawList)drawList);
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
        
        // Get camera matrix (view and projection)
        var camera = GetCameraMatrix(drawList.CameraData, renderSize);
        
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
        
        // Bind FBO
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboHandle);
        
        // Clear the screen
        GL.Viewport(0, 0, renderSize.X, renderSize.Y);
        GL.ClearColor(drawList.ClearColor);
        GL.ClearDepthf(0.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        // Set depth function
        GL.DepthFunc(DepthFunction.Greater);
        
        // Opaque pass, disable blending, enable depth testing
        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.DepthTest);
        
        // Draw each mesh
        RenderDrawDataList(opaqueDrawData, camera);
        
        // Transparent pass, enable blending, disable depth write
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Enable(EnableCap.Blend);
        GL.DepthMask(false);
        
        // Draw each mesh
        RenderDrawDataList(transparentDrawData, camera);
        
        // Restore depth write state
        GL.DepthMask(true);
        
        // Bind texture 1 to post-process FBO
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, postProcessFboHandle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, postProcessTextureHandle1, 0);
        
        // Blit FBO to post-process FBO
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fboHandle);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, postProcessFboHandle);
        GL.BlitFramebuffer(
            0, 0, 
            renderSize.X, renderSize.Y, 
            0, 0, 
            renderSize.X, renderSize.Y, 
            ClearBufferMask.ColorBufferBit, 
            BlitFramebufferFilter.Linear);
        
        // Process post-process effects
        var output = HandlePostProcessing(drawList.PostProcessingData, postProcessTextureHandle1, postProcessTextureHandle2);
        
        // Bind output texture to post-process FBO
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, postProcessFboHandle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, output, 0);
        
        // Clear screen
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, drawFramebuffer);
        GL.Viewport(0, 0, size.X, size.Y);
        GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        // Blit post-process FBO to screen
        var screenOffsetX = (size.X - renderSize.X) / 2;
        var screenOffsetY = (size.Y - renderSize.Y) / 2;
        
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, postProcessFboHandle);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, drawFramebuffer);
        GL.BlitFramebuffer(
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
    
    private int HandlePostProcessing(PostProcessingData data, int texture1, int texture2)
    {
        // Disable depth testing and blending
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.Blend);
        
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
    
    private void RenderDrawDataList(IReadOnlyList<DrawData> drawDataList, Matrix3 camera)
    {
        foreach (var drawData in drawDataList)
        {
            var transform = drawData.Transform * camera;
            var color1 = drawData.Color1;
            var color2 = drawData.Color2;
            
            switch (drawData.RenderType)
            {
                case RenderType.Mesh when drawData.Mesh is MeshHandle meshHandle:
                    // Use our program
                    GL.UseProgram(programHandle);
            
                    // Set transform
                    unsafe
                    {
                        GL.UniformMatrix3fv(mvpUniformLocation, 1, false, (float*)&transform);
                    }
            
                    GL.Uniform1f(zUniformLocation, RenderUtil.EncodeIntDepth(drawData.Index));
                    GL.Uniform1i(renderModeUniformLocation, (int) drawData.RenderMode);
                    GL.Uniform4f(color1UniformLocation, color1.X, color1.Y, color1.Z, color1.W);
                    GL.Uniform4f(color2UniformLocation, color2.X, color2.Y, color2.Z, color2.W);
            
                    // Bind our buffers
                    GL.BindVertexArray(meshHandle.VertexArrayHandle);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, meshHandle.IndexBufferHandle);
            
                    // Draw
                    GL.DrawElements(PrimitiveType.Triangles, meshHandle.Count, DrawElementsType.UnsignedInt, IntPtr.Zero);
                    break;
                case RenderType.Text when drawData.Text is TextHandle textHandle:
                    // Use our program
                    GL.UseProgram(glyphProgramHandle);
                    
                    unsafe
                    {
                        GL.UniformMatrix3fv(glyphMvpUniformLocation, 1, false, (float*)&transform);
                    }
                    GL.Uniform1f(glyphZUniformLocation, RenderUtil.EncodeIntDepth(drawData.Index));
                    
                    // Set textures
                    lock (registeredFonts)
                        foreach (var (i, fontHandle) in registeredFonts.Indexed())
                        {
                            GL.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + i));
                            GL.BindTexture(TextureTarget.Texture2d, fontHandle.AtlasHandle);
                            GL.Uniform1i(glyphFontAtlasesUniformLocation + i, i);
                        }
                    
                    // Set color
                    GL.Uniform4f(glyphBaseColorUniformLocation, color1.X, color1.Y, color1.Z, color1.W);

                    // Bind our buffers
                    GL.BindVertexArray(textVertexArrayHandle);
                    
                    // Update buffer data
                    var renderGlyphSize = Unsafe.SizeOf<RenderGlyph>();
                    var renderGlyphs = textHandle.Glyphs;
                    var renderGlyphsSize = renderGlyphs.Length * renderGlyphSize;
                    
                    GL.BindBuffer(BufferTarget.ArrayBuffer, textInstanceBufferHandle);
                    if (renderGlyphsSize > textBufferCurrentSize)
                    {
                        GL.BufferData(BufferTarget.ArrayBuffer, renderGlyphsSize, IntPtr.Zero, BufferUsage.DynamicDraw);
                        textBufferCurrentSize = renderGlyphsSize;
                    }
                    else
                    {
                        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, renderGlyphsSize, renderGlyphs);
                    }
                    
                    // Draw
                    GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, renderGlyphs.Length);
                    break;
            }
        }
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
        while (initializers.TryDequeue(out var initializer))
            initializer();
        
        UpdateFboData(size);
    }

    private void UpdateFboData(Vector2i size)
    {
        if (size == currentFboSize)
            return;
        
        // Delete old FBO data
        GL.DeleteRenderbuffer(fboColorBufferHandle);
        GL.DeleteRenderbuffer(fboDepthBufferHandle);
        
        // Create new FBO data
        fboColorBufferHandle = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, fboColorBufferHandle);
        GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, MsaaSamples, InternalFormat.Rgba16f, size.X, size.Y);
        
        fboDepthBufferHandle = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, fboDepthBufferHandle);
        GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, MsaaSamples, InternalFormat.DepthComponent32f, size.X, size.Y);
        
        // Bind new data to FBO
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboHandle);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, fboColorBufferHandle);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, fboDepthBufferHandle);        
        
        // Check FBO status
        var fboStatus = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (fboStatus != FramebufferStatus.FramebufferComplete)
            throw new InvalidOperationException($"Framebuffer is not complete: {fboStatus}");
        
        // Delete old post-process FBO data
        GL.DeleteTexture(postProcessTextureHandle1);
        GL.DeleteTexture(postProcessTextureHandle2);
        
        // Create new post-process FBO data
        postProcessTextureHandle1 = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, postProcessTextureHandle1);
        GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgba16f, size.X, size.Y);
        
        postProcessTextureHandle2 = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, postProcessTextureHandle2);
        GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgba16f, size.X, size.Y);
        
        // Check post-process FBO status
        var postProcessFboStatus = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (postProcessFboStatus != FramebufferStatus.FramebufferComplete)
            throw new InvalidOperationException($"Post-process framebuffer is not complete: {postProcessFboStatus}");
        
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