using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Platform;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering.PostProcessing;
using ParallelAnimationSystem.Rendering.TextProcessing;
using ParallelAnimationSystem.Util;
using TmpIO;
using TmpParser;

namespace ParallelAnimationSystem.Rendering;

public class Renderer(Options options, ILogger<Renderer> logger) : IDisposable
{
    private const int MaxFonts = 12;
    
    public bool ShouldExit { get; private set; }

    private WindowHandle? windowHandle;
    private OpenGLContextHandle? glContextHandle;
    
    public int QueuedDrawListCount => drawListQueue.Count;

    // Synchronization
    private readonly object meshDataLock = new();
    
    // Draw list pool
    private readonly ConcurrentQueue<DrawList> drawListPool = [];
    
    // Rendering data
    private readonly ConcurrentQueue<DrawList> drawListQueue = [];
    private readonly Buffer vertexBuffer = new();
    private readonly Buffer indexBuffer = new();
    private bool newMeshData = true;
    
    // Post processors
    private readonly Hue hue = new();
    private readonly Bloom bloom = new();
    
    // Graphics data
    private readonly List<FontData> registeredFonts = [];
    private MeshHandle baseFontMeshHandle;
    
    private int vertexArrayHandle;
    private int vertexBufferHandle;
    private int indexBufferHandle;
    private int multiDrawIndirectBufferHandle, multiDrawIndirectBufferSize;
    private int multiDrawStorageBufferHandle, multiDrawStorageBufferSize;
    private int multiDrawGlyphBufferHandle, multiDrawGlyphBufferSize;
    private int programHandle;
    private int fontAtlasesUniformLocation;
    
    private Vector2i currentFboSize;
    private int fboTextureHandle, fboDepthBufferHandle;
    private int fboHandle;
    private int postProcessTextureHandle1, postProcessTextureHandle2;
    private int postProcessFboHandle;

    private readonly List<DrawData> opaqueDrawData = [];
    private readonly List<DrawData> transparentDrawData = [];
    
    private readonly Buffer multiDrawIndirectBuffer = new();
    private readonly Buffer multiDrawStorageBuffer = new();
    private readonly Buffer multiDrawGlyphBuffer = new();
    
    public MeshHandle RegisterMesh(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<int> indices)
    {
        lock (meshDataLock)
        {
            newMeshData = true;

            var vertexOffset = vertexBuffer.Data.Length / Vector2.SizeInBytes;
            var indexOffset = indexBuffer.Data.Length / sizeof(int);
            var vertexSize = vertices.Length;
            var indexSize = indices.Length;
            
            vertexBuffer.Append(vertices);
            indexBuffer.Append(indices);
            
            logger.LogInformation("Registered mesh with {VertexSize} vertices and {IndexSize} indices", vertexSize, indexSize);
            
            return new MeshHandle(vertexOffset, vertexSize, indexOffset, indexSize);
        }
    }

    public FontHandle RegisterFont(Stream stream)
    {
        var fontFile = TmpRead.Read(stream);
        var fontData = new FontData(
            fontFile,
            fontFile.Characters
                .ToDictionary(x => x.Character, x => x.GlyphId),
            fontFile.Glyphs
                .ToDictionary(x => x.Id));
        lock (registeredFonts)
        {
            var handle = new FontHandle(registeredFonts.Count);
            registeredFonts.Add(fontData);
            logger.LogInformation("Registered font '{FontName}'", fontFile.Metadata.Name);
            return handle;
        }
    }

    public TextHandle CreateText(string str, IEnumerable<FontStack> fontStacks, string defaultFontName, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment)
    {
        lock (registeredFonts)
        {
            var textShaper = new TextShaper(registeredFonts, fontStacks.ToDictionary(x => x.Name.ToLowerInvariant()), defaultFontName);
            return new TextHandle(textShaper.ShapeText(str, horizontalAlignment, verticalAlignment).ToArray());
        }
    }
    
    public void Initialize()
    {
        var toolkitOptions = new ToolkitOptions
        {
            ApplicationName = "Parallel Animation System",
            Logger = new MelLogger<Renderer>(logger, "OpenGL: "),
        };
        Toolkit.Init(toolkitOptions);
        
        var contextSettings = new OpenGLGraphicsApiHints
        {
            Version = new Version(4, 6),
            Profile = OpenGLProfile.Core,
#if DEBUG
            DebugFlag = true,
#endif
            DepthBits = ContextDepthBits.None,
            StencilBits = ContextStencilBits.None,
        };

        EventQueue.EventRaised += (handle, type, args) =>
        {
            if (handle != windowHandle)
                return;

            if (type == PlatformEventType.Close)
            {
                logger.LogInformation("Closing window");
                
                var closeArgs = (CloseEventArgs) args;
                Toolkit.Window.Destroy(closeArgs.Window);
            }
        };

        windowHandle = Toolkit.Window.Create(contextSettings);
        Toolkit.Window.SetTitle(windowHandle, "Parallel Animation System");
        Toolkit.Window.SetClientSize(windowHandle, 1366, 768);
        Toolkit.Window.SetMode(windowHandle, WindowMode.Normal);
        
        // Create OpenGL context
        glContextHandle = Toolkit.OpenGL.CreateFromWindow(windowHandle);
        Toolkit.OpenGL.SetCurrentContext(glContextHandle);
        
        // Load OpenGL bindings
        GLLoader.LoadBindings(Toolkit.OpenGL.GetBindingsContext(glContextHandle));
        
        // Set VSync
        Toolkit.OpenGL.SetSwapInterval(options.VSync ? 1 : 0);
        
        logger.LogInformation("Window created");
        
        // Get window size
        Toolkit.Window.GetClientSize(windowHandle, out var initialWidth, out var initialHeight);
        var initialSize = new Vector2i(initialWidth, initialHeight);
        
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
        InitializeOpenGlData(initialSize);
        
        // Enable multisampling
        GL.Enable(EnableCap.Multisample);
        
        logger.LogInformation("OpenGL initialized");
    }

    public DrawList GetDrawList()
    {
        if (!drawListPool.TryDequeue(out var drawList))
            drawList = new DrawList();
        return drawList;
    }

    public void SubmitDrawList(DrawList drawList)
    {
        drawListQueue.Enqueue(drawList);
    }

    private void InitializeOpenGlData(Vector2i size)
    {
        // We will exclusively use DSA for this project
        vertexArrayHandle = GL.CreateVertexArray();
        vertexBufferHandle = GL.CreateBuffer();
        indexBufferHandle = GL.CreateBuffer();

        // Upload data to GPU
        var vertexBufferData = vertexBuffer.Data;
        var indexBufferData = indexBuffer.Data;
        var vertexBufferSize = vertexBufferData.Length * Vector2.SizeInBytes;
        var indexBufferSize = indexBufferData.Length * sizeof(int);
        
        GL.NamedBufferData(vertexBufferHandle, vertexBufferSize, vertexBuffer.Data, VertexBufferObjectUsage.StaticDraw);
        GL.NamedBufferData(indexBufferHandle, indexBufferSize, indexBuffer.Data, VertexBufferObjectUsage.StaticDraw);
        
        newMeshData = false;
        
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
        var vertexShaderSource = ResourceUtil.ReadAllText("Resources.Shaders.SimpleUnlitVertex.glsl");
        var fragmentShaderSource = ResourceUtil.ReadAllText("Resources.Shaders.SimpleUnlitFragment.glsl");
        
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
        GL.DetachShader(programHandle, vertexShader);
        GL.DetachShader(programHandle, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
        
        // Get uniform locations
        fontAtlasesUniformLocation = GL.GetUniformLocation(programHandle, "uFontAtlases");
        
        // Initialize fbos
        // Initialize scene fbo
        fboTextureHandle = GL.CreateTexture(TextureTarget.Texture2dMultisample);
        GL.TextureStorage2DMultisample(fboTextureHandle, 4, SizedInternalFormat.Rgba8, size.X, size.Y, true);
        
        fboDepthBufferHandle = GL.CreateRenderbuffer();
        GL.NamedRenderbufferStorageMultisample(fboDepthBufferHandle, 4, InternalFormat.DepthComponent32f, size.X, size.Y);
        
        fboHandle = GL.CreateFramebuffer();
        GL.NamedFramebufferTexture(fboHandle, FramebufferAttachment.ColorAttachment0, fboTextureHandle, 0);
        GL.NamedFramebufferRenderbuffer(fboHandle, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, fboDepthBufferHandle);
        
        // Initialize post process fbo
        postProcessTextureHandle1 = GL.CreateTexture(TextureTarget.Texture2d);
        GL.TextureStorage2D(postProcessTextureHandle1, 1, SizedInternalFormat.Rgba8, size.X, size.Y);
        
        postProcessTextureHandle2 = GL.CreateTexture(TextureTarget.Texture2d);
        GL.TextureStorage2D(postProcessTextureHandle2, 1, SizedInternalFormat.Rgba8, size.X, size.Y);
        
        postProcessFboHandle = GL.CreateFramebuffer();
        // We will bind the texture later
        
        currentFboSize = size;
        
        // Initialize post processors
        hue.Initialize();
        bloom.Initialize();
    }
    
    public void Dispose()
    {
        bloom.Dispose();
        hue.Dispose();

        lock (registeredFonts)
            foreach (var fontData in registeredFonts.Where(x => x.Initialized))
                GL.DeleteTexture(fontData.AtlasHandle);
        
        GL.DeleteBuffer(multiDrawIndirectBufferHandle);
        GL.DeleteBuffer(multiDrawStorageBufferHandle);
        GL.DeleteBuffer(multiDrawGlyphBufferHandle);
        
        GL.DeleteProgram(programHandle);
        GL.DeleteBuffer(vertexBufferHandle);
        GL.DeleteBuffer(indexBufferHandle);
        GL.DeleteVertexArray(vertexArrayHandle);
        
        GL.DeleteTexture(fboTextureHandle);
        GL.DeleteRenderbuffer(fboDepthBufferHandle);
        GL.DeleteTexture(postProcessTextureHandle1);
        GL.DeleteTexture(postProcessTextureHandle2);
        
        GL.DeleteFramebuffer(fboHandle);
        GL.DeleteFramebuffer(postProcessFboHandle);
        
        if (windowHandle is not null && !Toolkit.Window.IsWindowDestroyed(windowHandle))
            Toolkit.Window.Destroy(windowHandle);
    }

    public void ProcessFrame()
    {
        if (windowHandle is null)
            throw new InvalidOperationException("Renderer has not been initialized");
        
        Toolkit.Window.ProcessEvents(false);
        
        // Check if window is closed
        if (Toolkit.Window.IsWindowDestroyed(windowHandle))
        {
            ShouldExit = true;
            return;
        }
        
        // Get window size
        Toolkit.Window.GetClientSize(windowHandle, out var width, out var height);
        var size = new Vector2i(width, height);
        
        RenderFrame(size);
    }
    
    private void RenderFrame(Vector2i size)
    {
        // Get the current draw list
        // If no draw list is available, wait until we have one
        DrawList? drawList;
        while (!drawListQueue.TryDequeue(out drawList))
            Thread.Yield();

        // If window size is invalid, don't draw
        // and limit FPS
        if (size.X <= 0 || size.Y <= 0)
        {
            Thread.Sleep(50);
            return;
        }

        // Update OpenGL data
        UpdateOpenGlData(size);
        
        // Split draw list into opaque and transparent
        opaqueDrawData.Clear();
        transparentDrawData.Clear();
        
        foreach (var drawData in drawList)
        {
            // Discard draw data that is fully transparent, or outside of clipping range
            if ((drawData.Color1.W == 0.0f && drawData.Color2.W == 0.0f) || drawData.Z < 0.0f || drawData.Z > 1.0f)
                continue;
            
            // Add to appropriate list
            if (drawData.RenderType != RenderType.Text && drawData.Color1.W == 1.0f && drawData.Color2.W == 1.0f)
                opaqueDrawData.Add(drawData);
            else
                transparentDrawData.Add(drawData);
        }
        
        // Sort transparent draw data back to front and opaque front to back
        opaqueDrawData.Sort((a, b) => a.Z.CompareTo(b.Z));
        transparentDrawData.Sort((a, b) => b.Z.CompareTo(a.Z));
        
        // Get camera matrix (view and projection)
        var camera = GetCameraMatrix(drawList.CameraData, size);
        
        // Render
        GL.Viewport(0, 0, currentFboSize.X, currentFboSize.Y);
        
        // Clear buffers
        var clearColor = drawList.ClearColor;
        var depth = 1.0f;
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
            }
        }
        
        // Bind indirect buffer
        GL.BindBuffer(BufferTarget.DrawIndirectBuffer, multiDrawIndirectBufferHandle);
        
        // Bind storage buffer
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, multiDrawStorageBufferHandle);
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, multiDrawGlyphBufferHandle);
        
        // Bind our vertex array
        GL.BindVertexArray(vertexArrayHandle);
        
        // Opaque pass, disable blending, enable depth testing
        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.DepthTest);
        
        // Render opaque draw data
        RenderDrawDataList(opaqueDrawData, camera);
        
        // Transparent pass, enable blending, disable depth write
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
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
            currentFboSize.X, currentFboSize.Y,
            0, 0,
            currentFboSize.X, currentFboSize.Y,
            ClearBufferMask.ColorBufferBit,
            BlitFramebufferFilter.Linear);
        
        // Do post-processing
        var finalTexture = HandlePostProcessing(drawList.PostProcessingData, postProcessTextureHandle1, postProcessTextureHandle2);
        
        // Bind final texture to post process fbo
        GL.NamedFramebufferTexture(postProcessFboHandle, FramebufferAttachment.ColorAttachment0, finalTexture, 0);
        
        // Blit to window
        GL.BlitNamedFramebuffer(
            postProcessFboHandle, 0,
            0, 0,
            currentFboSize.X, currentFboSize.Y,
            0, 0,
            size.X, size.Y,
            ClearBufferMask.ColorBufferBit,
            BlitFramebufferFilter.Linear);
        
        Debug.Assert(glContextHandle is not null);
        Toolkit.OpenGL.SwapBuffers(glContextHandle);
        
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
        
        // Append data
        foreach (var drawData in drawDataList)
        {
            var renderType = drawData.RenderType;
            var mesh = drawData.Mesh;
            var text = drawData.Text;
            var transform = drawData.Transform;
            var color1 = drawData.Color1;
            var color2 = drawData.Color2;
            var z = drawData.Z;
            var renderMode = drawData.RenderMode;
            
            var mvp = transform * camera;

            multiDrawIndirectBuffer.Append(new DrawElementsIndirectCommand
            {
                Count = renderType == RenderType.Text ? baseFontMeshHandle.IndexCount : mesh.IndexCount,
                InstanceCount = renderType == RenderType.Text ? text.Glyphs.Length : 1,
                FirstIndex = renderType == RenderType.Text ? baseFontMeshHandle.IndexOffset : mesh.IndexOffset,
                BaseVertex = renderType == RenderType.Text ? baseFontMeshHandle.VertexOffset : mesh.VertexOffset,
                BaseInstance = 0
            });
            
            multiDrawStorageBuffer.Append(new MultiDrawItem
            {
                MvpRow1 = mvp.Row0,
                MvpRow2 = mvp.Row1,
                MvpRow3 = mvp.Row2,
                Color1 = (Vector4) color1,
                Color2 = (Vector4) color2,
                Z = z,
                RenderMode = (int) renderMode,
                RenderType = (int) renderType,
                GlyphOffset = multiDrawGlyphBuffer.Data.Length / Unsafe.SizeOf<RenderGlyph>(),
            });
            
            if (renderType == RenderType.Text)
                multiDrawGlyphBuffer.Append<RenderGlyph>(text.Glyphs);
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
        if (hue.Process(currentFboSize, data.HueShiftAngle, texture1, texture2))
            Swap(ref texture1, ref texture2); // Swap if we processed
        
        if (bloom.Process(currentFboSize, data.BloomIntensity, data.BloomDiffusion, texture1, texture2))
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
        GL.DeleteTexture(fboTextureHandle);
        GL.DeleteRenderbuffer(fboDepthBufferHandle);
        GL.DeleteTexture(postProcessTextureHandle1);
        GL.DeleteTexture(postProcessTextureHandle2);
        
        // Create new textures
        fboTextureHandle = GL.CreateTexture(TextureTarget.Texture2dMultisample);
        GL.TextureStorage2DMultisample(fboTextureHandle, 4, SizedInternalFormat.Rgba8, size.X, size.Y, true);
        
        fboDepthBufferHandle = GL.CreateRenderbuffer();
        GL.NamedRenderbufferStorageMultisample(fboDepthBufferHandle, 4, InternalFormat.DepthComponent32f, size.X, size.Y);
        
        postProcessTextureHandle1 = GL.CreateTexture(TextureTarget.Texture2d);
        GL.TextureStorage2D(postProcessTextureHandle1, 1, SizedInternalFormat.Rgba8, size.X, size.Y);
        
        postProcessTextureHandle2 = GL.CreateTexture(TextureTarget.Texture2d);
        GL.TextureStorage2D(postProcessTextureHandle2, 1, SizedInternalFormat.Rgba8, size.X, size.Y);
        
        // Bind to fbo
        GL.NamedFramebufferTexture(fboHandle, FramebufferAttachment.ColorAttachment0, fboTextureHandle, 0);
        GL.NamedFramebufferRenderbuffer(fboHandle, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, fboDepthBufferHandle);

        currentFboSize = size;
        
        logger.LogInformation("OpenGL framebuffer size updated, now at {Width}x{Height}", currentFboSize.X, currentFboSize.Y);
    }
    
    private static void Swap<T>(ref T a, ref T b)
    {
        (a, b) = (b, a);
    }
}