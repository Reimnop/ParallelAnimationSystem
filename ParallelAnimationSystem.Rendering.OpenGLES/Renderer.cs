using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGLES2;
using System.Numerics;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering.Common;
using ParallelAnimationSystem.Rendering.OpenGLES.PostProcessing;
using ParallelAnimationSystem.Util;
using ParallelAnimationSystem.Windowing;
using ParallelAnimationSystem.Windowing.OpenGL;

namespace ParallelAnimationSystem.Rendering.OpenGLES;

public class Renderer : IRenderer, IDisposable
{
    private const int MaxFontsCount = 12;
    private const int MsaaSamples = 4;
    
    private record struct MeshInfo(int IndexOffset, int IndexCount);
    private record struct FontInfo(int AtlasHandle);
    
    public IWindow Window => window;

    private readonly IOpenGLWindow window;
    
    // Graphics data
    private readonly Buffer<Vector2> vertexBuffer = new();
    private readonly Buffer<int> indexBuffer = new();
    private readonly List<MeshInfo?> meshInfos = [];

    private readonly List<FontInfo?> fontInfos = [];
    
    private readonly int programHandle;
    private readonly int mvpUniformLocation, zUniformLocation, renderModeUniformLocation, color1UniformLocation, color2UniformLocation;
    private readonly int glyphProgramHandle;
    private readonly int glyphMvpUniformLocation, glyphZUniformLocation, glyphFontAtlasesUniformLocation, glyphBaseColorUniformLocation;
    
    private int mainVertexArrayHandle, mainVertexBufferHandle, mainIndexBufferHandle;

    private readonly int textVertexArrayHandle, textInstanceBufferHandle;
    private int textBufferCurrentSize;

    private int fboColorBufferHandle, fboDepthBufferHandle;
    private readonly int fboHandle;
    private int postProcessTextureHandle1, postProcessTextureHandle2;
    private readonly int postProcessFboHandle;
    private Vector2i currentFboSize;
    
    // Post-processing
    private readonly Bloom bloom;
    private readonly UberPost uberPost;
    
    // Temporary draw data lists
    private readonly List<DrawList.DrawData> opaqueDrawData = [];
    private readonly List<DrawList.DrawData> transparentDrawData = [];

    private readonly AppSettings appSettings;
    private readonly IncomingResourceQueue incomingResourceQueue;
    private readonly ILogger<Renderer> logger;
    
    public Renderer(
        AppSettings appSettings,
        IWindowManager windowManager,
        ResourceLoader loader,
        IncomingResourceQueue incomingResourceQueue,
        ILogger<Renderer> logger)
    {
        this.appSettings = appSettings;
        this.incomingResourceQueue = incomingResourceQueue;
        this.logger = logger;
        
        logger.LogInformation("Initializing OpenGL ES renderer");
        
        // Create window
        window = (IOpenGLWindow) windowManager.CreateWindow(
            "Parallel Animation System",
            new OpenGLSettings
            {
                MajorVersion = 3,
                MinorVersion = 0,
                IsES = true,
            });
        window.MakeContextCurrent();
        
        // Load OpenGL bindings
        GLLoader.LoadBindings(new WMBindingsContext((IOpenGLWindowManager) windowManager));
        
        logger.LogInformation("Window created");
        
        // Log OpenGL information
        logger.LogInformation("OpenGL ES: {Version}", GL.GetString(StringName.Version));
        logger.LogInformation("Renderer: {Renderer}", GL.GetString(StringName.Renderer));
        logger.LogInformation("Vendor: {Vendor}", GL.GetString(StringName.Vendor));
        logger.LogInformation("Shading language: {ShadingLanguage}", GL.GetString(StringName.ShadingLanguageVersion));
        
        #region OpenGL Data Initialization

        {
            var initialSize = Window.FramebufferSize;
            
            // Create main program handle
            programHandle = CreateShaderProgram(loader, "Shaders/UnlitVertex.glsl", "Shaders/UnlitFragment.glsl");

            // Get uniform locations
            mvpUniformLocation = GL.GetUniformLocation(programHandle, "uMvp");
            zUniformLocation = GL.GetUniformLocation(programHandle, "uZ");
            renderModeUniformLocation = GL.GetUniformLocation(programHandle, "uRenderMode");
            color1UniformLocation = GL.GetUniformLocation(programHandle, "uColor1");
            color2UniformLocation = GL.GetUniformLocation(programHandle, "uColor2");

            // Create glyph program handle
            glyphProgramHandle = CreateShaderProgram(loader, "Shaders/TextVertex.glsl", "Shaders/TextFragment.glsl");

            // Get glyph uniform locations
            glyphMvpUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uMvp");
            glyphZUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uZ");
            glyphFontAtlasesUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uFontAtlases");
            glyphBaseColorUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uBaseColor");
            
            // Set font atlas texture unit uniforms
            GL.UseProgram(glyphProgramHandle);
            for (var i = 0; i < MaxFontsCount; i++)
                GL.Uniform1i(glyphFontAtlasesUniformLocation + i, i);

            // Initialize text buffers
            var renderGlyphSize = Unsafe.SizeOf<RenderGlyph>();

            textInstanceBufferHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, textInstanceBufferHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, 1024 * renderGlyphSize, IntPtr.Zero, BufferUsage.DynamicDraw);
            textBufferCurrentSize = 1024;

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
            GL.VertexAttribIPointer(6, 1, VertexAttribIType.Int, renderGlyphSize, 52); // FontId

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
            var vertexShaderSource = loader.ReadResourceString("Shaders/PostProcessVertex.glsl");
            if (vertexShaderSource is null)
                throw new InvalidOperationException("Could not load post-processing vertex shader source");
            
            var vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);

            var vertexShaderCompileStatus = GL.GetShaderi(vertexShader, ShaderParameterName.CompileStatus);
            if (vertexShaderCompileStatus == 0)
            {
                GL.GetShaderInfoLog(vertexShader, out var infoLog);
                throw new InvalidOperationException($"Failed to compile vertex shader: {infoLog}");
            }

            uberPost = new UberPost(loader, vertexShader);
            bloom = new Bloom(loader, vertexShader);

            // Clean up
            GL.DeleteShader(vertexShader);
        }

        #endregion
    }

    private static int CreateShaderProgram(ResourceLoader loader, string vertexShaderResourceName, string fragmentShaderResourceName)
    {
        // Initialize shader program
        var vertexShaderSource = loader.ReadResourceString(vertexShaderResourceName);
        if (vertexShaderSource is null)
            throw new InvalidOperationException($"Could not load vertex shader source from resource '{vertexShaderResourceName}'");
        
        var fragmentShaderSource = loader.ReadResourceString(fragmentShaderResourceName);
        if (fragmentShaderSource is null)
            throw new InvalidOperationException($"Could not load fragment shader source from resource '{fragmentShaderResourceName}'");
        
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
    
    public void ProcessFrame(IDrawList drawList)
    {
        var oglDrawList = (DrawList) drawList;
        
        var size = window.FramebufferSize;
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
        
        // Set context
        window.MakeContextCurrent();
        
        // Update OpenGL data
        UpdateOpenGlData(renderSize);
        
        // Get camera matrix (view and projection)
        var camera = RenderUtil.GetCameraMatrix(drawList.CameraData, renderSize);
        
        // Split draw list into opaque and transparent
        opaqueDrawData.Clear();
        transparentDrawData.Clear();
        
        foreach (var drawData in oglDrawList)
        {
            // Discard draw data that is fully transparent
            if (drawData.Color1.A == 0.0f && drawData.Color2.A == 0.0f)
                continue;
            
            // Add to appropriate list
            if (drawData.RenderType != RenderType.Text && drawData.Color1.A == 1.0f && (drawData.Color2.A == 1.0f || drawData.RenderMode == RenderMode.Normal))
                opaqueDrawData.Add(drawData);
            else
                transparentDrawData.Add(drawData);
        }
        
        // Reverse opaque draw data list so that it is drawn
        // from back to front to avoid overdraw
        opaqueDrawData.Reverse();
        
        // Bind FBO
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboHandle);
        
        // Bind atlas texture
        for (var i = 0; i < fontInfos.Count; i++)
        {
            var fontInfoNullable = fontInfos[i];
            if (!fontInfoNullable.HasValue)
                continue;
                        
            var fontInfo = fontInfoNullable.Value;
            GL.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + i));
            GL.BindTexture(TextureTarget.Texture2d, fontInfo.AtlasHandle);
        }
        
        // Clear the screen
        GL.Viewport(0, 0, renderSize.X, renderSize.Y);
        var clearColor = drawList.ClearColor;
        GL.ClearColor(clearColor.R, clearColor.G, clearColor.B, clearColor.A);
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
        
        // Present to window
        var screenOffset = new Vector2i(
            (size.X - renderSize.X) / 2,
            (size.Y - renderSize.Y) / 2);
        
        window.Present(postProcessFboHandle, Vector4.Zero, renderSize, screenOffset);
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
    
    private void RenderDrawDataList(IReadOnlyList<DrawList.DrawData> drawDataList, Matrix3x2 camera)
    {
        var currentProgram = -1;
        var currentVertexArray = -1;
        
        foreach (var drawData in drawDataList)
        {
            var transform = drawData.Transform * camera;
            var color1 = drawData.Color1;
            var color2 = drawData.Color2;
            
            switch (drawData.RenderType)
            {
                case RenderType.Mesh:
                    var mesh = drawData.Mesh;
                    Debug.Assert(mesh is not null);
                    
                    var meshInfoNullable = meshInfos[mesh.Id];
                    Debug.Assert(meshInfoNullable.HasValue);
                    
                    var meshInfo = meshInfoNullable.Value;
                    
                    // Use our program
                    if (currentProgram != programHandle)
                    {
                        currentProgram = programHandle;
                        GL.UseProgram(programHandle);
                    }
            
                    // Set transform
                    unsafe
                    {
                        GL.UniformMatrix3x2fv(mvpUniformLocation, 1, false, (float*)&transform);
                    }
            
                    GL.Uniform1f(zUniformLocation, RenderUtil.EncodeIntDepth(drawData.Index));
                    GL.Uniform1i(renderModeUniformLocation, (int) drawData.RenderMode);
                    GL.Uniform4f(color1UniformLocation, color1.R, color1.G, color1.B, color1.A);
                    GL.Uniform4f(color2UniformLocation, color2.R, color2.G, color2.B, color2.A);
            
                    // Bind our buffers
                    if (currentVertexArray != mainVertexArrayHandle)
                    {
                        currentVertexArray = mainVertexArrayHandle;
                        GL.BindVertexArray(mainVertexArrayHandle);
                        GL.BindBuffer(BufferTarget.ElementArrayBuffer, mainIndexBufferHandle);
                    }
            
                    // Draw
                    GL.DrawElements(PrimitiveType.Triangles, meshInfo.IndexCount, DrawElementsType.UnsignedInt, meshInfo.IndexOffset * sizeof(uint));
                    break;
                case RenderType.Text:
                    var textHandle = drawData.Text;
                    Debug.Assert(textHandle is not null);
                    
                    // Use our program
                    if (currentProgram != glyphProgramHandle)
                    {
                        currentProgram = glyphProgramHandle;
                        GL.UseProgram(glyphProgramHandle);
                    }
                    
                    unsafe
                    {
                        GL.UniformMatrix3x2fv(glyphMvpUniformLocation, 1, false, (float*)&transform);
                    }
                    GL.Uniform1f(glyphZUniformLocation, RenderUtil.EncodeIntDepth(drawData.Index));
                    
                    // Set color
                    GL.Uniform4f(glyphBaseColorUniformLocation, color1.R, color1.G, color1.B, color1.A);
                    
                    // Update buffer data
                    var renderGlyphSize = Unsafe.SizeOf<RenderGlyph>();
                    var renderGlyphs = textHandle.Glyphs;
                    
                    GL.BindBuffer(BufferTarget.ArrayBuffer, textInstanceBufferHandle);
                    if (renderGlyphs.Length > textBufferCurrentSize)
                    {
                        GL.BufferData(BufferTarget.ArrayBuffer, renderGlyphs.Length * renderGlyphSize, renderGlyphs, BufferUsage.DynamicDraw);
                        textBufferCurrentSize = renderGlyphs.Length;
                    }
                    else
                    {
                        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, renderGlyphs.Length * renderGlyphSize, renderGlyphs);
                    }
                    
                    // Bind our VAO
                    if (currentVertexArray != textVertexArrayHandle)
                    {
                        currentVertexArray = textVertexArrayHandle;
                        GL.BindVertexArray(textVertexArrayHandle);
                    }
                    
                    // Draw
                    GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, renderGlyphs.Length);
                    break;
            }
        }
    }
    
    private void UpdateOpenGlData(Vector2i size)
    {
        UpdateMeshData();
        UpdateFontData();
        UpdateFboData(size);
    }

    private void UpdateFontData()
    {
        if (!incomingResourceQueue.TryDequeueAllFonts(out var incomingFonts))
            return;

        foreach (var incomingFont in incomingFonts)
        {
            var atlas = incomingFont.Atlas;
            
            // Create texture
            var atlasHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2d, atlasHandle);
            GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgb32f, atlas.Width, atlas.Height);
            GL.TexSubImage2D(TextureTarget.Texture2d, 0, 0, 0, atlas.Width, atlas.Height, PixelFormat.Rgb, PixelType.Float, atlas.Data);
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            
            fontInfos.EnsureCount(incomingFont.FontId + 1);
            fontInfos[incomingFont.FontId] = new FontInfo(atlasHandle);
        }
        
        logger.LogInformation("Updated {FontCount} font atlases", incomingFonts.Count);
    }

    private void UpdateMeshData()
    {
        if (!incomingResourceQueue.TryDequeueAllMeshes(out var incomingMeshes))
            return;
        
        foreach (var incomingMesh in incomingMeshes)
        {
            var vertices = incomingMesh.Vertices;
            var indices = incomingMesh.Indices;

            for (var i = 0; i < indices.Length; i++)
                indices[i] += vertexBuffer.Length; // Offset indices
            
            var meshInfo = new MeshInfo(indexBuffer.Length, indices.Length);
            meshInfos.EnsureCount(incomingMesh.MeshId + 1);
            meshInfos[incomingMesh.MeshId] = meshInfo;
            
            vertexBuffer.Append(vertices);
            indexBuffer.Append(indices);
        }
        
        // Update OpenGL buffers
        
        // Initialize vertex buffer if needed
        if (mainVertexBufferHandle == 0)
            mainVertexBufferHandle = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, mainVertexBufferHandle);
        GL.BufferData(BufferTarget.ArrayBuffer, vertexBuffer.LengthInBytes, vertexBuffer.Data, BufferUsage.DynamicDraw);
        
        // Initialize vertex array if needed
        if (mainVertexArrayHandle == 0)
        {
            mainVertexArrayHandle = GL.GenVertexArray();
            GL.BindVertexArray(mainVertexArrayHandle);
            
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Unsafe.SizeOf<Vector2>(), 0);
        }
        
        // Initialize index buffer if needed
        if (mainIndexBufferHandle == 0)
            mainIndexBufferHandle = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, mainIndexBufferHandle);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indexBuffer.LengthInBytes, indexBuffer.Data, BufferUsage.DynamicDraw);
        
        logger.LogInformation("OpenGL mesh buffers updated, now at {VertexCount} vertices and {IndexCount} indices", 
            vertexBuffer.Length,
            indexBuffer.Length);
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
        if (Window is IDisposable disposable)
            disposable.Dispose();
    }
    
    private static void Swap<T>(ref T a, ref T b)
    {
        (a, b) = (b, a);
    }
}