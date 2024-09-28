using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGLES2;
using OpenTK.Mathematics;
using OpenTK.Platform;
using OpenTK.Platform.Native;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering.TextProcessing;
using ParallelAnimationSystem.Util;
using TmpIO;
using TmpParser;

namespace ParallelAnimationSystem.Rendering.OpenGLES;

public class Renderer(Options options, IResourceManager resourceManager, ILogger<Renderer> logger) : IRenderer
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
    
    public bool ShouldExit { get; private set; }
    public int QueuedDrawListCount => drawListQueue.Count;
    
    // Draw list stuff
    private readonly ConcurrentQueue<DrawList> drawListPool = [];
    private readonly ConcurrentQueue<DrawList> drawListQueue = [];
    
    // Graphics data
    private readonly ConcurrentQueue<Action> meshInitializers = [];
    
    private readonly List<FontHandle> registeredFonts = [];
    private IMeshHandle? baseFontMeshHandle;
    
    private int programHandle;
    private int mvpUniformLocation, zUniformLocation, renderModeUniformLocation, color1UniformLocation, color2UniformLocation;
    
    private int fboTextureHandle, fboDepthBufferHandle;
    private int fboHandle;
    private Vector2i currentFboSize;
    
    // Temporary draw data lists
    private readonly List<DrawData> opaqueDrawData = [];
    private readonly List<DrawData> transparentDrawData = [];
    
    private WindowHandle? windowHandle;
    private OpenGLContextHandle? glContextHandle;
    
    public void Initialize()
    {
        logger.LogInformation("Starting renderer via ANGLE");
        
        // Forcing ANGLE to be used on Windows
        PlatformComponents.PreferANGLE = true;
        
        var toolkitOptions = new ToolkitOptions
        {
            ApplicationName = "Parallel Animation System",
            Logger = new MelLogger<Renderer>(logger, "OpenGL ES: "),
        };
        Toolkit.Init(toolkitOptions);

        var contextSettings = new OpenGLGraphicsApiHints
        {
            Version = new Version(3, 0), // ES 3.0
#if DEBUG
            DebugFlag = true,
#endif
            DepthBits = ContextDepthBits.Depth32,
            StencilBits = ContextStencilBits.Stencil1,
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
                
                ShouldExit = true;
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
        Toolkit.Window.GetFramebufferSize(windowHandle, out var initialWidth, out var initialHeight);
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
        
        // Log OpenGL information
        logger.LogInformation("OpenGL ES: {Version}", GL.GetString(StringName.Version));
        logger.LogInformation("Renderer: {Renderer}", GL.GetString(StringName.Renderer));
        logger.LogInformation("Vendor: {Vendor}", GL.GetString(StringName.Vendor));
        logger.LogInformation("Shading language: {ShadingLanguage}", GL.GetString(StringName.ShadingLanguageVersion));
    }

    private void InitializeOpenGlData(Vector2i initialSize)
    {
        // Initialize shader program
        var vertexShaderSource = resourceManager.LoadGraphicsResourceString("Shaders/UberVertex.glsl");
        var fragmentShaderSource = resourceManager.LoadGraphicsResourceString("Shaders/UberFragment.glsl");
        
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
        
        programHandle = GL.CreateProgram();
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
        
        // Get uniform locations
        mvpUniformLocation = GL.GetUniformLocation(programHandle, "uMvp");
        zUniformLocation = GL.GetUniformLocation(programHandle, "uZ");
        renderModeUniformLocation = GL.GetUniformLocation(programHandle, "uRenderMode");
        color1UniformLocation = GL.GetUniformLocation(programHandle, "uColor1");
        color2UniformLocation = GL.GetUniformLocation(programHandle, "uColor2");
        
        // Initialize FBO
        fboTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, fboTextureHandle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, initialSize.X, initialSize.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);
        
        fboDepthBufferHandle = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, fboDepthBufferHandle);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent32f, initialSize.X, initialSize.Y);
        
        fboHandle = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboHandle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, fboTextureHandle, 0);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, fboDepthBufferHandle);
        
        // Check FBO status
        var fboStatus = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (fboStatus != FramebufferStatus.FramebufferComplete)
            throw new InvalidOperationException($"Framebuffer is not complete: {fboStatus}");
        
        currentFboSize = initialSize;
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
        
        meshInitializers.Enqueue(intializer);
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
        // TODO: Implement text rendering
        return null;
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
        Toolkit.Window.GetFramebufferSize(windowHandle, out var width, out var height);
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
        
        // Get camera matrix (view and projection)
        var camera = GetCameraMatrix(drawList.CameraData, size);
        
        // Split draw list into opaque and transparent
        opaqueDrawData.Clear();
        transparentDrawData.Clear();
        
        foreach (var drawData in drawList)
        {
            // Discard draw data that is fully transparent, or outside of clipping range
            if ((drawData.Color1.W == 0.0f && drawData.Color2.W == 0.0f) || drawData.Z < 0.0f || drawData.Z > 1.0f)
                continue;
            
            // Add to appropriate list
            if (drawData.RenderType != RenderType.Text && drawData.Color1.W == 1.0f && (drawData.Color2.W == 1.0f || drawData.RenderMode == RenderMode.Normal))
                opaqueDrawData.Add(drawData);
            else
                transparentDrawData.Add(drawData);
        }
        
        // Sort transparent draw data back to front and opaque front to back
        opaqueDrawData.Sort((a, b) => a.Z.CompareTo(b.Z));
        transparentDrawData.Sort((a, b) => b.Z.CompareTo(a.Z));
        
        // Bind FBO
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboHandle);
        
        // Clear the screen
        GL.Viewport(0, 0, size.X, size.Y);
        GL.ClearColor(drawList.ClearColor);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        // Use our program
        GL.UseProgram(programHandle);
        
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
        
        // Blit FBO to screen
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fboHandle);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        GL.BlitFramebuffer(0, 0, size.X, size.Y, 0, 0, size.X, size.Y, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
        
        // Swap buffers
        Debug.Assert(glContextHandle is not null);
        Toolkit.OpenGL.SwapBuffers(glContextHandle);
    }

    private void RenderDrawDataList(IReadOnlyList<DrawData> drawDataList, Matrix3 camera)
    {
        foreach (var drawData in drawDataList)
        {
            var mesh = drawData.Mesh;
            if (mesh is not MeshHandle meshHandle)
                continue;
            
            var transform = drawData.Transform * camera;
            var color1 = drawData.Color1;
            var color2 = drawData.Color2;
            
            // Set transform
            unsafe
            {
                GL.UniformMatrix3fv(mvpUniformLocation, 1, false, (float*)&transform);
            }
            
            GL.Uniform1f(zUniformLocation, drawData.Z);
            GL.Uniform1i(renderModeUniformLocation, (int) drawData.RenderMode);
            GL.Uniform4f(color1UniformLocation, color1.X, color1.Y, color1.Z, color1.W);
            GL.Uniform4f(color2UniformLocation, color2.X, color2.Y, color2.Z, color2.W);
            
            // Bind our buffers
            GL.BindVertexArray(meshHandle.VertexArrayHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, meshHandle.IndexBufferHandle);
            
            // Draw
            GL.DrawElements(PrimitiveType.Triangles, meshHandle.Count, DrawElementsType.UnsignedInt, IntPtr.Zero);
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
        UpdateMeshData();
        UpdateFontData();
        UpdateFboData(size);
    }

    private void UpdateMeshData()
    {
        while (meshInitializers.TryDequeue(out var initializer))
            initializer();
    }
    
    private void UpdateFontData()
    {
    }

    private void UpdateFboData(Vector2i size)
    {
        if (size == currentFboSize)
            return;
        
        // Delete old FBO data
        GL.DeleteTexture(fboTextureHandle);
        GL.DeleteRenderbuffer(fboDepthBufferHandle);
        
        // Create new FBO data
        fboTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, fboTextureHandle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, size.X, size.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        
        fboDepthBufferHandle = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, fboDepthBufferHandle);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent32f, size.X, size.Y);
        
        // Bind new data to FBO
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboHandle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, fboTextureHandle, 0);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, fboDepthBufferHandle);
        
        // Check FBO status
        var fboStatus = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (fboStatus != FramebufferStatus.FramebufferComplete)
            throw new InvalidOperationException($"Framebuffer is not complete: {fboStatus}");
        
        currentFboSize = size;
        
        logger.LogInformation("OpenGL framebuffer size updated, now at {Width}x{Height}", currentFboSize.X, currentFboSize.Y);
    }

    public void Dispose()
    {
        if (windowHandle is not null && !Toolkit.Window.IsWindowDestroyed(windowHandle))
            Toolkit.Window.Destroy(windowHandle);
    }
}