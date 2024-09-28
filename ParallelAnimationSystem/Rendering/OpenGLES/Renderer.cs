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
    private readonly ConcurrentQueue<Action> initializers = [];
    
    private readonly List<FontHandle> registeredFonts = [];
    private IMeshHandle? baseFontMeshHandle;
    
    private int programHandle;
    private int mvpUniformLocation, zUniformLocation, renderModeUniformLocation, color1UniformLocation, color2UniformLocation;
    private int glyphProgramHandle;
    private int 
        glyphMvpUniformLocation, 
        glyphZUniformLocation, 
        glyphMinMaxUniformLocation, 
        glyphUvUniformLocation, 
        glyphBoldItalicUniformLocation,
        glyphFontAtlasesUniformLocation,
        glyphGlyphColorUniformLocation,
        glyphFontIndexUniformLocation;
    
    private int fboColorBufferHandle, fboDepthBufferHandle;
    private int fboHandle;
    private int postProcessTextureHandle;
    private int postProcessFboHandle;
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
        glyphMinMaxUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uMinMax");
        glyphUvUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uUv");
        glyphBoldItalicUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uBoldItalic");
        glyphFontAtlasesUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uFontAtlases");
        glyphGlyphColorUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uGlyphColor");
        glyphFontIndexUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uFontIndex");
        
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
        postProcessTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, postProcessTextureHandle);
        GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgba16f, initialSize.X, initialSize.Y);
        
        postProcessFboHandle = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, postProcessFboHandle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, postProcessTextureHandle, 0);
        
        // Check post-process FBO status
        var postProcessFboStatus = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (postProcessFboStatus != FramebufferStatus.FramebufferComplete)
            throw new InvalidOperationException($"Post-process framebuffer is not complete: {postProcessFboStatus}");
        
        currentFboSize = initialSize;
    }

    private int CreateShaderProgram(string vertexShaderResourceName, string fragmentShaderResourceName)
    {
        // Initialize shader program
        var vertexShaderSource = resourceManager.LoadGraphicsResourceString(vertexShaderResourceName);
        var fragmentShaderSource = resourceManager.LoadGraphicsResourceString(fragmentShaderResourceName);
        
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
        
        // Blit FBO to post-process FBO
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fboHandle);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, postProcessFboHandle);
        GL.BlitFramebuffer(
            0, 0, 
            size.X, size.Y, 
            0, 0, 
            size.X, size.Y, 
            ClearBufferMask.ColorBufferBit, 
            BlitFramebufferFilter.Linear);
        
        // Blit post-process FBO to screen
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, postProcessFboHandle);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        GL.BlitFramebuffer(
            0, 0, 
            size.X, size.Y, 
            0, 0, 
            size.X, size.Y, 
            ClearBufferMask.ColorBufferBit, 
            BlitFramebufferFilter.Linear);
        
        // Swap buffers
        Debug.Assert(glContextHandle is not null);
        Toolkit.OpenGL.SwapBuffers(glContextHandle);
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
            
                    GL.Uniform1f(zUniformLocation, drawData.Z);
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
                    GL.Uniform1f(glyphZUniformLocation, drawData.Z);
                    
                    // Set textures
                    lock (registeredFonts)
                        foreach (var (i, fontHandle) in registeredFonts.Indexed())
                        {
                            GL.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + i));
                            GL.BindTexture(TextureTarget.Texture2d, fontHandle.AtlasHandle);
                            GL.Uniform1i(glyphFontAtlasesUniformLocation + i, i);
                        }
                    
                    var baseFontMeshHandle = this.baseFontMeshHandle as MeshHandle;
                    Debug.Assert(baseFontMeshHandle is not null);

                    foreach (var glyph in textHandle.Glyphs)
                    {
                        var glyphColor = new Vector4(
                            float.IsNaN(glyph.Color.X) ? color1.X : glyph.Color.X,
                            float.IsNaN(glyph.Color.Y) ? color1.Y : glyph.Color.Y,
                            float.IsNaN(glyph.Color.Z) ? color1.Z : glyph.Color.Z,
                            float.IsNaN(glyph.Color.W) ? color1.W : MathF.Min(glyph.Color.W, color1.W));
                        GL.Uniform4f(glyphGlyphColorUniformLocation, glyphColor.X, glyphColor.Y, glyphColor.Z, glyphColor.W);
                        GL.Uniform4f(glyphMinMaxUniformLocation, glyph.Min.X, glyph.Min.Y, glyph.Max.X, glyph.Max.Y);
                        GL.Uniform4f(glyphUvUniformLocation, glyph.MinUV.X, glyph.MinUV.Y, glyph.MaxUV.X, glyph.MaxUV.Y);
                        GL.Uniform1i(glyphBoldItalicUniformLocation, (int)glyph.BoldItalic);
                        GL.Uniform1i(glyphFontIndexUniformLocation, glyph.FontIndex);
                        
                        GL.BindVertexArray(baseFontMeshHandle.VertexArrayHandle);
                        GL.BindBuffer(BufferTarget.ElementArrayBuffer, baseFontMeshHandle.IndexBufferHandle);
                        
                        GL.DrawElements(PrimitiveType.Triangles, baseFontMeshHandle.Count, DrawElementsType.UnsignedInt, IntPtr.Zero);
                    }
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
        GL.DeleteTexture(postProcessTextureHandle);
        
        // Create new post-process FBO data
        postProcessTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, postProcessTextureHandle);
        GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgba16f, size.X, size.Y);
        
        // Bind new data to FBO
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, postProcessFboHandle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, postProcessTextureHandle, 0);
        
        // Check post-process FBO status
        var postProcessFboStatus = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (postProcessFboStatus != FramebufferStatus.FramebufferComplete)
            throw new InvalidOperationException($"Post-process framebuffer is not complete: {postProcessFboStatus}");
        
        currentFboSize = size;
        
        logger.LogInformation("OpenGL framebuffer size updated, now at {Width}x{Height}", currentFboSize.X, currentFboSize.Y);
    }

    public void Dispose()
    {
        if (windowHandle is not null && !Toolkit.Window.IsWindowDestroyed(windowHandle))
            Toolkit.Window.Destroy(windowHandle);
    }
}