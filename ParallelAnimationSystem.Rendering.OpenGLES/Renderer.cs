using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGLES2;
using System.Numerics;
using System.Runtime.InteropServices;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering.Common;
using ParallelAnimationSystem.Rendering.Data;
using ParallelAnimationSystem.Rendering.OpenGLES.PostProcessing;
using ParallelAnimationSystem.Util;
using ParallelAnimationSystem.Windowing;
using ParallelAnimationSystem.Windowing.OpenGL;

namespace ParallelAnimationSystem.Rendering.OpenGLES;

public class Renderer : IRenderer, IDisposable
{
    private const int MaxFontsCount = 12;
    private const int MsaaSamples = 4;
    private const int MaxOverlays = 10;

    private struct MeshInfo
    {
        public int IndexOffset;
        public int IndexCount;
    }

    private struct FontInfo
    {
        public int AtlasTextureHandle;
    }
    
    public struct TextInfo
    {
        public int GlyphOffset;
        public int GlyphCount;
    }
    
    private struct DrawCommandWithDepth
    {
        public DrawType DrawType;
        public int DrawId;
        public float Depth;
    }
    
    private readonly IOpenGLWindow window;
    
    // Rendering data
    private readonly Buffer<Vector2> vertexBuffer = new();
    private readonly Buffer<int> indexBuffer = new();
    private readonly Buffer<RenderGlyph> glyphBuffer = new();
    
    private readonly List<MeshInfo> meshInfos = [];
    private readonly FontInfo[] fontInfos = new FontInfo[MaxFontsCount];
    private readonly List<TextInfo> textInfos = [];
    
    private readonly int programHandle;
    private readonly int mvpUniformLocation, zUniformLocation, renderModeUniformLocation, color1UniformLocation, color2UniformLocation;
    private readonly int glyphProgramHandle;
    private readonly int glyphMvpUniformLocation, glyphZUniformLocation, glyphBaseColorUniformLocation;
    
    private int mainVertexArrayHandle, mainVertexBufferHandle, mainIndexBufferHandle;

    private readonly int textVertexArrayHandle, textAtlasSamplerHandle;
    private int textInstanceBufferHandle;

    private Vector2i currentFboSize;
    private int fboColorBufferHandle, fboDepthBufferHandle;
    private readonly int fboHandle;
    private int postProcessTextureHandle1, postProcessTextureHandle2;
    private readonly int postProcessFboHandle;

    private Vector2i currentOverlaySize;
    private readonly int overlayProgram;
    private readonly int overlayOffsetUniformLocation, overlayScaleUniformLocation;
    private readonly int overlaySampler;
    private readonly int overlayFboHandle;
    private int overlayTexture;

    private readonly int emptyVao;
    
    // Post-processing
    private readonly LegacyBloom legacyBloom;
    private readonly UniversalBloom universalBloom;
    private readonly UberPost uberPost;
    
    // Temporary draw data lists
    private readonly List<DrawCommandWithDepth> opaqueDrawCommands = [];
    private readonly List<DrawCommandWithDepth> transparentDrawCommands = [];

    // Overlay renderers
    private readonly List<IOverlayRenderer> overlayRenderers = [];
    
    // Dirty flags
    private bool meshBufferDirty = true;
    private bool fontsDirty = true; // there is a way to not reload all fonts, but I'm lazy and this is not gonna be a bottleneck so who cares
    private bool textsDirty = true;
    
    // Injected dependencies
    private readonly AppSettings appSettings;
    private readonly RenderingFactory renderingFactory;
    private readonly ILogger<Renderer> logger;
    
    public Renderer(
        AppSettings appSettings,
        IRenderingFactory renderingFactory,
        IWindow window,
        ResourceLoader loader,
        ILogger<Renderer> logger)
    {
        this.appSettings = appSettings;
        this.renderingFactory = (RenderingFactory) renderingFactory;
        this.window = (IOpenGLWindow) window;
        this.logger = logger;
        
        logger.LogInformation("Initializing OpenGL ES renderer");
        
        // Create window
        this.window.MakeContextCurrent();
        
        // Load OpenGL bindings
        GLLoader.LoadBindings(new BindingsContext(this.window));
        
        // Log OpenGL information
        logger.LogInformation("OpenGL ES: {Version}", GL.GetString(StringName.Version));
        logger.LogInformation("Renderer: {Renderer}", GL.GetString(StringName.Renderer));
        logger.LogInformation("Vendor: {Vendor}", GL.GetString(StringName.Vendor));
        logger.LogInformation("Shading language: {ShadingLanguage}", GL.GetString(StringName.ShadingLanguageVersion));
        
        #region OpenGL Data Initialization

        {
            var initialSize = this.window.FramebufferSize;
            
            // Create main program handle
            programHandle = LoaderUtil.LoadShaderProgram(loader, "UnlitVertex", "UnlitFragment");

            // Get uniform locations
            mvpUniformLocation = GL.GetUniformLocation(programHandle, "uMvp");
            zUniformLocation = GL.GetUniformLocation(programHandle, "uZ");
            renderModeUniformLocation = GL.GetUniformLocation(programHandle, "uRenderMode");
            color1UniformLocation = GL.GetUniformLocation(programHandle, "uColor1");
            color2UniformLocation = GL.GetUniformLocation(programHandle, "uColor2");

            // Create glyph program handle
            glyphProgramHandle = LoaderUtil.LoadShaderProgram(loader, "TextVertex", "TextFragment");

            // Get glyph uniform locations
            glyphMvpUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uMvp");
            glyphZUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uZ");
            glyphBaseColorUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uBaseColor");
            
            var glyphFontAtlasesUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uFontAtlases");
            
            // Set font atlas texture unit uniforms
            GL.UseProgram(glyphProgramHandle);
            for (var i = 0; i < MaxFontsCount; i++)
                GL.Uniform1i(glyphFontAtlasesUniformLocation + i, i);
            
            // Create text atlas sampler
            textAtlasSamplerHandle = GL.GenSampler();
            GL.SamplerParameteri(textAtlasSamplerHandle, SamplerParameterI.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.SamplerParameteri(textAtlasSamplerHandle, SamplerParameterI.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.SamplerParameteri(textAtlasSamplerHandle, SamplerParameterI.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.SamplerParameteri(textAtlasSamplerHandle, SamplerParameterI.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            // Initialize text vertex array
            textVertexArrayHandle = GL.GenVertexArray();
            GL.BindVertexArray(textVertexArrayHandle);

            // Enable vertex attributes
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);
            GL.EnableVertexAttribArray(3);
            GL.EnableVertexAttribArray(4);
            GL.EnableVertexAttribArray(5);
            GL.EnableVertexAttribArray(6);

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
            
            // Initialize overlay resources
            
            // Load overlay program
            overlayProgram = LoaderUtil.LoadShaderProgram(loader, "OverlayVertex", "OverlayFragment");
            overlayOffsetUniformLocation = GL.GetUniformLocation(overlayProgram, "uOffset");
            overlayScaleUniformLocation = GL.GetUniformLocation(overlayProgram, "uScale");
            
            // Create overlay sampler
            overlaySampler = GL.GenSampler();
            GL.SamplerParameteri(overlaySampler, SamplerParameterI.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.SamplerParameteri(overlaySampler, SamplerParameterI.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.SamplerParameteri(overlaySampler, SamplerParameterI.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            GL.SamplerParameteri(overlaySampler, SamplerParameterI.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

            var borderColor = Vector4.Zero;
            GL.SamplerParameterf(overlaySampler, SamplerParameterF.TextureBorderColor, in borderColor.X);
            
            // Create overlay texture
            overlayTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2d, overlayTexture);
            GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgba16f, initialSize.X, initialSize.Y);
            
            // Create overlay FBO
            overlayFboHandle = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, overlayFboHandle);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, overlayTexture, 0);
            
            // Check overlay FBO status
            var overlayFboStatus = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (overlayFboStatus != FramebufferStatus.FramebufferComplete)
                throw new InvalidOperationException($"Overlay framebuffer is not complete: {overlayFboStatus}");
            
            currentOverlaySize = initialSize;
            
            // Create empty VAO
            emptyVao = GL.GenVertexArray();

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

            legacyBloom = new LegacyBloom(loader, vertexShader);
            universalBloom = new UniversalBloom(loader, vertexShader);
            uberPost = new UberPost(loader, vertexShader);

            // Clean up
            GL.DeleteShader(vertexShader);
        }

        #endregion
        
        // Subscribe to events
        this.renderingFactory.Meshes.ItemInserted += OnMeshInserted;
        this.renderingFactory.Meshes.ItemRemoved += OnMeshRemoved;
        this.renderingFactory.Fonts.ItemInserted += OnFontInserted;
        this.renderingFactory.Fonts.ItemRemoved += OnFontRemoved;
        this.renderingFactory.Texts.ItemInserted += OnTextInserted;
        this.renderingFactory.Texts.ItemRemoved += OnTextRemoved;
    }
    
    public void Dispose()
    {
        logger.LogInformation("Disposing OpenGL ES renderer");
        
        // Unsubscribe from events
        renderingFactory.Meshes.ItemInserted -= OnMeshInserted;
        renderingFactory.Meshes.ItemRemoved -= OnMeshRemoved;
        renderingFactory.Fonts.ItemInserted -= OnFontInserted;
        renderingFactory.Fonts.ItemRemoved -= OnFontRemoved;
        renderingFactory.Texts.ItemInserted -= OnTextInserted;
        renderingFactory.Texts.ItemRemoved -= OnTextRemoved;
        
        window.MakeContextCurrent();
        
        // Delete GL resources
        GL.DeleteProgram(programHandle);
        GL.DeleteProgram(glyphProgramHandle);
        
        GL.DeleteBuffer(mainVertexBufferHandle);
        GL.DeleteBuffer(mainIndexBufferHandle);
        GL.DeleteVertexArray(mainVertexArrayHandle);
        
        GL.DeleteBuffer(textInstanceBufferHandle);
        GL.DeleteVertexArray(textVertexArrayHandle);
        GL.DeleteSampler(textAtlasSamplerHandle);
        
        GL.DeleteRenderbuffer(fboColorBufferHandle);
        GL.DeleteRenderbuffer(fboDepthBufferHandle);
        GL.DeleteFramebuffer(fboHandle);
        
        GL.DeleteTexture(postProcessTextureHandle1);
        GL.DeleteTexture(postProcessTextureHandle2);
        GL.DeleteFramebuffer(postProcessFboHandle);
        
        // Delete font atlas textures
        foreach (var fontInfo in fontInfos)
        {
            if (fontInfo.AtlasTextureHandle == 0)
                continue;
            
            GL.DeleteTexture(fontInfo.AtlasTextureHandle);
        }
        
        GL.DeleteSampler(overlaySampler);
        GL.DeleteTexture(overlayTexture);
        GL.DeleteFramebuffer(overlayFboHandle);
        GL.DeleteProgram(overlayProgram);
        
        GL.DeleteVertexArray(emptyVao);
        
        legacyBloom.Dispose();
        universalBloom.Dispose();
        uberPost.Dispose();
    }
    
    private void OnTextInserted(object? sender, ObservableSparseSetEventArgs<Text> e)
    {
        textsDirty = true;
    }

    private void OnTextRemoved(object? sender, ObservableSparseSetEventArgs<Text> e)
    {
        textsDirty = true;
    }
    
    private void OnFontInserted(object? sender, ObservableSparseSetEventArgs<Font> e)
    {
        fontsDirty = true;
    }
    
    private void OnFontRemoved(object? sender, ObservableSparseSetEventArgs<Font> e)
    {
        fontsDirty = true;
    }
    
    private void OnMeshInserted(object? sender, ObservableSparseSetEventArgs<Mesh> e)
    {
        meshBufferDirty = true;
    }

    private void OnMeshRemoved(object? sender, ObservableSparseSetEventArgs<Mesh> e)
    {
        meshBufferDirty = true;
    }

    public void AddOverlayRenderer(IOverlayRenderer overlayRenderer)
    {
        overlayRenderers.Add(overlayRenderer);
    }
    
    public bool RemoveOverlayRenderer(IOverlayRenderer overlayRenderer)
    {
        return overlayRenderers.Remove(overlayRenderer);
    }
    
    public void ProcessFrame(IDrawDataProvider drawDataProvider)
    {
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
        
        // Get draw data
        var drawData = drawDataProvider.DrawData;
        
        // Get camera matrix (view and projection)
        var camera = RenderUtil.GetCameraMatrix(drawData.CameraData, renderSize);
        
        // Split draw list into opaque and transparent
        opaqueDrawCommands.Clear();
        transparentDrawCommands.Clear();
        
        var currentDepthInt = 0;
        foreach (var drawCommand in drawData.DrawCommands)
        {
            // Add to appropriate list
            if (RenderUtil.ShouldUseTransparentDrawList(drawCommand, drawData))
            {
                transparentDrawCommands.Add(new DrawCommandWithDepth
                {
                    DrawType = drawCommand.DrawType,
                    DrawId = drawCommand.DrawId,
                    Depth = currentDepthInt / (float)drawData.DrawCommands.Length
                });
            }
            else
            {
                opaqueDrawCommands.Add(new DrawCommandWithDepth
                {
                    DrawType = drawCommand.DrawType,
                    DrawId = drawCommand.DrawId,
                    Depth = currentDepthInt / (float)drawData.DrawCommands.Length
                });
            }

            currentDepthInt++;
        }
        
        // Reverse opaque draw data list so that it is drawn
        // from back to front to avoid overdraw
        opaqueDrawCommands.Reverse();
        
        // Bind FBO
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboHandle);
        
        // Bind atlas texture
        for (var i = 0; i < fontInfos.Length; i++)
        {
            ref var fontInfo = ref fontInfos[i];
            if (fontInfo.AtlasTextureHandle == 0)
                continue;
            
            GL.ActiveTexture((TextureUnit)((int) TextureUnit.Texture0 + i));
            GL.BindTexture(TextureTarget.Texture2d, fontInfo.AtlasTextureHandle);
            GL.BindSampler((uint) i, textAtlasSamplerHandle);
        }
        
        // Clear the screen
        GL.Viewport(0, 0, renderSize.X, renderSize.Y);
        var clearColor = drawData.ClearColor;
        GL.ClearColor(clearColor.R, clearColor.G, clearColor.B, clearColor.A);
        GL.ClearDepthf(0.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        // Set depth function
        GL.DepthFunc(DepthFunction.Greater);
        
        // Opaque pass, disable blending, enable depth testing
        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.DepthTest);
        
        // Draw each mesh
        RenderDrawDataList(
            CollectionsMarshal.AsSpan(opaqueDrawCommands),
            drawData,
            camera);
        
        // Transparent pass, enable blending, disable depth write
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Enable(EnableCap.Blend);
        GL.DepthMask(false);
        
        // Draw each mesh
        RenderDrawDataList(
            CollectionsMarshal.AsSpan(transparentDrawCommands),
            drawData,
            camera);
        
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
        var finalTexture = HandlePostProcessing(drawData.PostProcessingData, postProcessTextureHandle1, postProcessTextureHandle2);
        
        // Render overlays
        var firstOverlayOffsetXInt = (size.X - renderSize.X) / 2;
        var firstOverlayOffsetYInt = (size.Y - renderSize.Y) / 2;
        var firstOverlayOffsetX = firstOverlayOffsetXInt / (float) size.X;
        var firstOverlayOffsetY = firstOverlayOffsetYInt / (float) size.Y;
        
        var firstOverlayScaleX = renderSize.X / (float) size.X;
        var firstOverlayScaleY = renderSize.Y / (float) size.Y;
        
        Span<Vector2> overlayOffsets = stackalloc Vector2[MaxOverlays];
        overlayOffsets[0] = new Vector2(firstOverlayOffsetX, firstOverlayOffsetY);
        
        Span<Vector2> overlayScales = stackalloc Vector2[MaxOverlays];
        overlayScales[0] = new Vector2(firstOverlayScaleX, firstOverlayScaleY);
        
        Span<int> overlayTextures = stackalloc int[MaxOverlays];
        overlayTextures[0] = finalTexture;
        
        var overlayCount = 1;
        foreach (var overlayRenderer in overlayRenderers)
        {
            if (overlayCount >= MaxOverlays)
                break;
            
            var texture = overlayRenderer.ProcessFrame(size);
            if (texture == 0)
                continue;

            overlayOffsets[overlayCount] = Vector2.Zero;
            overlayScales[overlayCount] = Vector2.One;
            overlayTextures[overlayCount] = texture;
            overlayCount++;
        }
        
        // Update overlay texture if needed
        if (currentOverlaySize != size)
        {
            currentOverlaySize = size;
            
            GL.DeleteTexture(overlayTexture);
            
            overlayTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2d, overlayTexture);
            GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgba16f, size.X, size.Y);
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, overlayFboHandle);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, overlayTexture, 0);
        }
        
        // Bind our framebuffer
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, overlayFboHandle);
        
        // Clear overlay FBO
        GL.Viewport(0, 0, size.X, size.Y);
        GL.ClearColor(0f, 0f, 0f, 0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        // Set blending
        GL.Enable(EnableCap.Blend);
        GL.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
        
        // Bind our stuff
        GL.UseProgram(overlayProgram);
        GL.BindVertexArray(emptyVao);
        GL.BindSampler(0, overlaySampler);
        
        // Render each overlay
        for (var i = 0; i < overlayCount; i++)
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, overlayTextures[i]);
            
            // Set uniforms
            var offset = overlayOffsets[i];
            var scale = overlayScales[i];
            
            GL.Uniform2f(overlayOffsetUniformLocation, offset.X, offset.Y);
            GL.Uniform2f(overlayScaleUniformLocation, scale.X, scale.Y);
            
            // Draw
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }
        
        // Present to window
        window.Present(overlayFboHandle, Vector4.Zero, size, Vector2i.Zero);
    }
    
    private int HandlePostProcessing(PostProcessingData data, int texture1, int texture2)
    {
        // Disable depth testing and blending
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.Blend);
        
        if (legacyBloom.Process(currentFboSize, data.LegacyBloom.Intensity, data.LegacyBloom.Diffusion, texture1, texture2))
            Swap(ref texture1, ref texture2);
        
        if (universalBloom.Process(currentFboSize, data.UniversalBloom.Intensity, data.UniversalBloom.Diffusion, texture1, texture2))
            Swap(ref texture1, ref texture2);
        
        if (uberPost.Process(
                currentFboSize,
                data.Time,
                data.HueShift.Angle,
                data.LensDistortion.Intensity, data.LensDistortion.Center,
                data.ChromaticAberration.Intensity,
                data.Vignette.Center, data.Vignette.Intensity, data.Vignette.Rounded, data.Vignette.Roundness, data.Vignette.Smoothness, data.Vignette.Color,
                data.Gradient.Color1, data.Gradient.Color2, data.Gradient.Intensity, data.Gradient.Rotation, data.Gradient.Mode,
                data.Glitch.Intensity, data.Glitch.Speed, data.Glitch.Size,
                texture1, texture2))
            Swap(ref texture1, ref texture2);
        
        return texture1;
    }
    
    private void RenderDrawDataList(in Span<DrawCommandWithDepth> drawCommands, in DrawData drawData, in Matrix3x2 camera)
    {
        var meshInfosSpan = CollectionsMarshal.AsSpan(meshInfos);
        var textInfosSpan = CollectionsMarshal.AsSpan(textInfos);
        
        foreach (ref var drawCommand in drawCommands)
        {
            switch (drawCommand.DrawType)
            {
                case DrawType.Mesh:
                {
                    ref var meshDrawItem = ref drawData.MeshDrawItems[drawCommand.DrawId];
                    ref var meshInfo = ref meshInfosSpan[meshDrawItem.MeshHandle.Id];
                    
                    var mvp = meshDrawItem.Transform * camera;
                    
                    // Use our program
                    GL.UseProgram(programHandle);
            
                    // Set transform
                    unsafe
                    {
                        GL.UniformMatrix3x2fv(mvpUniformLocation, 1, false, (float*)&mvp);
                    }
            
                    GL.Uniform1f(zUniformLocation, drawCommand.Depth);
                    GL.Uniform1i(renderModeUniformLocation, (int) meshDrawItem.RenderMode);
                    GL.Uniform4f(color1UniformLocation, meshDrawItem.Color1.R, meshDrawItem.Color1.G, meshDrawItem.Color1.B, meshDrawItem.Color1.A);
                    GL.Uniform4f(color2UniformLocation, meshDrawItem.Color2.R, meshDrawItem.Color2.G, meshDrawItem.Color2.B, meshDrawItem.Color2.A);
            
                    // Bind our buffers
                    GL.BindVertexArray(mainVertexArrayHandle);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, mainIndexBufferHandle);
            
                    // Draw
                    GL.DrawElements(PrimitiveType.Triangles, meshInfo.IndexCount, DrawElementsType.UnsignedInt, meshInfo.IndexOffset * sizeof(uint));
                    break;
                }
                case DrawType.Text:
                {
                    ref var textDrawItem = ref drawData.TextDrawItems[drawCommand.DrawId];
                    ref var textInfo = ref textInfosSpan[textDrawItem.TextHandle.Id];
                    
                    var mvp = textDrawItem.Transform * camera;
                    
                    // Use our program
                    GL.UseProgram(glyphProgramHandle);
                    
                    unsafe
                    {
                        GL.UniformMatrix3x2fv(glyphMvpUniformLocation, 1, false, (float*)&mvp);
                    }
                    GL.Uniform1f(glyphZUniformLocation, drawCommand.Depth);
                    
                    // Set color
                    GL.Uniform4f(glyphBaseColorUniformLocation, textDrawItem.Color.R, textDrawItem.Color.G, textDrawItem.Color.B, textDrawItem.Color.A);
                    
                    // Bind our VAO
                    GL.BindVertexArray(textVertexArrayHandle);
                    
                    // Set instance offsets
                    GL.BindBuffer(BufferTarget.ArrayBuffer, textInstanceBufferHandle);
                    
                    var renderGlyphSize = Unsafe.SizeOf<RenderGlyph>();
                    var instanceOffset = textInfo.GlyphOffset * renderGlyphSize;
                    GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, renderGlyphSize, instanceOffset); // Min
                    GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, renderGlyphSize, instanceOffset + 8); // Max
                    GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, renderGlyphSize, instanceOffset + 16); // MinUV
                    GL.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, renderGlyphSize, instanceOffset + 24); // MaxUV
                    GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, renderGlyphSize, instanceOffset + 32); // Color
                    GL.VertexAttribIPointer(5, 1, VertexAttribIType.Int, renderGlyphSize, instanceOffset + 48); // BoldItalic
                    GL.VertexAttribIPointer(6, 1, VertexAttribIType.Int, renderGlyphSize, instanceOffset + 52); // FontId
                    
                    // Draw
                    GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, textInfo.GlyphCount);
                    break;
                }
            }
        }
    }
    
    private void UpdateOpenGlData(Vector2i size)
    {
        UpdateMeshData();
        UpdateFontData();
        UpdateTextData();
        UpdateFboData(size);
    }
    
    private void UpdateMeshData()
    {
        if (!meshBufferDirty)
            return;
        
        meshBufferDirty = false;
        
        // Clear existing data
        vertexBuffer.Clear();
        indexBuffer.Clear();
        
        // Rebuild mesh buffer
        if (renderingFactory.Meshes.Count > 0)
        {
            var maxId = renderingFactory.Meshes.Select(x => x.Key).Max();
            meshInfos.EnsureCount(maxId + 1);
        
            var meshInfosSpan = CollectionsMarshal.AsSpan(meshInfos);
            foreach (var (id, mesh) in renderingFactory.Meshes)
            {
                var vertices = mesh.Vertices;
                var indices = mesh.Indices.ToArray();
                for (var i = 0; i < indices.Length; i++)
                    indices[i] += vertexBuffer.Length; // Offset indices
                
                ref var meshInfo = ref meshInfosSpan[id];
                meshInfo.IndexOffset = indexBuffer.Length;
                meshInfo.IndexCount = indices.Length;
            
                vertexBuffer.Append(vertices);
                indexBuffer.Append(indices);
            }

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
        }
        
        logger.LogInformation("Mesh buffer updated, registered {VertexCount} vertices and {IndexCount} indices", 
            vertexBuffer.Length,
            indexBuffer.Length);
    }
    
    private void UpdateFontData()
    {
        if (!fontsDirty)
            return;
        
        fontsDirty = false;
        
        // Free all existing font atlas textures
        foreach (ref var fontInfo in fontInfos.AsSpan())
        {
            if (fontInfo.AtlasTextureHandle == 0)
                continue;
            
            GL.DeleteTexture(fontInfo.AtlasTextureHandle);
            fontInfo.AtlasTextureHandle = 0;
        }

        // Rebuild font atlases
        if (renderingFactory.Fonts.Count > 0)
        {
            var maxId = renderingFactory.Fonts.Select(x => x.Key).Max();
            if (maxId >= fontInfos.Length)
                throw new InvalidOperationException($"Exceeded maximum number of fonts ({fontInfos.Length}) supported by the renderer");
            
            foreach (var (id, font) in renderingFactory.Fonts)
            {
                GL.PixelStorei(PixelStoreParameter.UnpackAlignment, 2);
            
                // Create texture
                var atlasHandle = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2d, atlasHandle);
                GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgb16f, font.Width, font.Height);
                GL.TexSubImage2D(TextureTarget.Texture2d, 0, 0, 0, font.Width, font.Height, PixelFormat.Rgb, PixelType.HalfFloat, font.Atlas);
            
                // Put it in our font info list
                ref var fontInfo = ref fontInfos[id];
                fontInfo.AtlasTextureHandle = atlasHandle;
            }
        }
        
        logger.LogInformation("Font atlases updated, registered {FontCount} fonts", renderingFactory.Fonts.Count);
    }

    private void UpdateTextData()
    {
        if (!textsDirty)
            return;

        textsDirty = false;
        
        // Clear existing data
        glyphBuffer.Clear();
        
        // Rebuild text buffer
        if (renderingFactory.Texts.Count > 0)
        {
            var maxId = renderingFactory.Texts.Select(x => x.Key).Max();
            textInfos.EnsureCount(maxId + 1);
        
            var textInfosSpan = CollectionsMarshal.AsSpan(textInfos);
            foreach (var (id, text) in renderingFactory.Texts)
            {
                ref var textInfo = ref textInfosSpan[id];
                textInfo.GlyphOffset = glyphBuffer.Length;
                textInfo.GlyphCount = text.Glyphs.Length;
            
                glyphBuffer.Append(text.Glyphs);
            }

            // Update our buffer with the new data
            if (textInstanceBufferHandle == 0)
                textInstanceBufferHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, textInstanceBufferHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, glyphBuffer.LengthInBytes, glyphBuffer.Data, BufferUsage.DynamicDraw);
        }
        
        logger.LogInformation("Text buffer updated, registered {GlyphCount} glyphs", glyphBuffer.Length);
    }

    private void UpdateFboData(Vector2i size)
    {
        if (size == currentFboSize)
            return;
        
        if (size.X * size.Y == 0)
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
    
    private static void Swap<T>(ref T a, ref T b)
    {
        (a, b) = (b, a);
    }
}