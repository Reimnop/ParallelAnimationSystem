using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Numerics;
using System.Runtime.InteropServices;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering.Common;
using ParallelAnimationSystem.Rendering.Data;
using ParallelAnimationSystem.Rendering.OpenGL.PostProcessing;
using ParallelAnimationSystem.Util;
using ParallelAnimationSystem.Windowing;
using ParallelAnimationSystem.Windowing.OpenGL;

namespace ParallelAnimationSystem.Rendering.OpenGL;

public class Renderer : IRenderer, IDisposable
{
    private struct MeshInfo
    {
        public int VertexOffset;
        public int IndexOffset;
        public int IndexCount;
    }

    private struct FontInfo
    {
        public int AtlasTextureHandle;
    }

    private struct TextInfo
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
    
    private const int MaxFontsCount = 12;
    private const int MsaaSamples = 4;
    private const int MaxOverlays = 10;
    
    // Rendering data
    private readonly Buffer<Vector2> vertexBuffer = new();
    private readonly Buffer<int> indexBuffer = new();
    private readonly Buffer<RenderGlyph> glyphBuffer = new();
    
    private readonly List<MeshInfo> meshInfos = [];
    private readonly FontInfo[] fontInfos = new FontInfo[MaxFontsCount];
    private readonly List<TextInfo> textInfos = [];
    
    // Post processors
    private readonly LegacyBloom legacyBloom;
    private readonly UniversalBloom universalBloom;
    private readonly Glitch glitch;
    private readonly UberPost uberPost;
    
    // Graphics data
    private readonly int baseFontVertexOffset = 0, baseFontIndexOffset = 0, baseFontIndexCount = 6;
    
    private readonly int vertexArrayHandle, vertexBufferHandle, indexBufferHandle;
    private readonly int glyphStorageBufferHandle;
    
    private readonly int multiDrawIndirectBufferHandle;
    private int multiDrawIndirectBufferSize;
    private readonly int multiDrawStorageBufferHandle;
    private int multiDrawStorageBufferSize;
    private readonly int programHandle;
    private readonly int fontAtlasesUniformLocation;
    private readonly int fontAtlasSampler;
    
    private Vector2i currentFboSize;
    private int fboColorBufferHandle, fboDepthBufferHandle;
    private readonly int fboHandle;
    private int postProcessTextureHandle1, postProcessTextureHandle2;
    private readonly int postProcessFboHandle;

    private Vector2i currentOverlaySize;
    private readonly int overlayProgram;
    private readonly int overlaySourceOffsetsUniformLocation, overlaySourceScalesUniformLocation, overlaySourceCountUniformLocation;
    private readonly int overlaySampler;
    private int overlayTexture;

    private readonly int overlayFramebufferHandle;

    private readonly List<DrawCommandWithDepth> opaqueDrawCommands = [];
    private readonly List<DrawCommandWithDepth> transparentDrawCommands = [];
    
    private readonly Buffer<DrawElementsIndirectCommand> multiDrawIndirectBuffer = new();
    private readonly Buffer<MultiDrawItem> multiDrawStorageBuffer = new();
    
    // Overlay renderers
    private readonly List<IOverlayRenderer> overlayRenderers = [];
    
    // Dirty flags
    private bool meshBufferDirty = true;
    private bool fontsDirty = true; // there is a way to not reload all fonts, but I'm lazy and this is not gonna be a bottleneck so who cares
    private bool textsDirty = true;

    // Injected dependencies
    private readonly AppSettings appSettings;
    private readonly RenderingFactory renderingFactory;
    private readonly IOpenGLWindow window;
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
        
        logger.LogInformation("Initializing OpenGL renderer");
        
        this.window.MakeContextCurrent();
        
        // Load OpenGL bindings
        GLLoader.LoadBindings(new BindingsContext(this.window));
        
        // Enable multisampling
        GL.Enable(EnableCap.Multisample);
        
        // Log OpenGL info
        logger.LogInformation("OpenGL: {Version}", GL.GetString(StringName.Version));
        logger.LogInformation("Renderer: {Renderer}", GL.GetString(StringName.Renderer));
        logger.LogInformation("Vendor: {Vendor}", GL.GetString(StringName.Vendor));
        logger.LogInformation("Shading language: {ShadingLanguage}", GL.GetString(StringName.ShadingLanguageVersion));
        
        #region OpenGL Data Initialization

        {
            var size = window.FramebufferSize;
            
            // Create vertex array and buffers
            vertexArrayHandle = GL.CreateVertexArray();
            vertexBufferHandle = GL.CreateBuffer();
            indexBufferHandle = GL.CreateBuffer();
            
            // Bind buffers to vertex array
            GL.EnableVertexArrayAttrib(vertexArrayHandle, 0);
            GL.VertexArrayVertexBuffer(vertexArrayHandle, 0, vertexBufferHandle, IntPtr.Zero, Unsafe.SizeOf<Vector2>());
            GL.VertexArrayAttribFormat(vertexArrayHandle, 0, 2, VertexAttribType.Float, false, 0);
            GL.VertexArrayAttribBinding(vertexArrayHandle, 0, 0);

            GL.VertexArrayElementBuffer(vertexArrayHandle, indexBufferHandle);
            
            // Create glyph storage buffer
            glyphStorageBufferHandle = GL.CreateBuffer();

            // Initialize multi draw buffer
            multiDrawIndirectBufferHandle = GL.CreateBuffer();
            multiDrawStorageBufferHandle = GL.CreateBuffer();

            // Initialize shader program
            programHandle = LoaderUtil.LoadShaderProgram(loader, "UberVertex", "UberFragment");

            // Get uniform locations
            fontAtlasesUniformLocation = GL.GetUniformLocation(programHandle, "uFontAtlases");

            // Initialize font atlas sampler
            fontAtlasSampler = GL.CreateSampler();
            GL.SamplerParameteri(fontAtlasSampler, SamplerParameterI.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.SamplerParameteri(fontAtlasSampler, SamplerParameterI.TextureMagFilter, (int)TextureMagFilter.Linear);

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
            
            // Initialize overlay resources
            
            // Load overlay program
            overlayProgram = LoaderUtil.LoadComputeProgram(loader, "Overlay");
            overlaySourceOffsetsUniformLocation = GL.GetUniformLocation(overlayProgram, "uSourceOffsets");
            overlaySourceScalesUniformLocation = GL.GetUniformLocation(overlayProgram, "uSourceScales");
            overlaySourceCountUniformLocation = GL.GetUniformLocation(overlayProgram, "uSourceCount");
            
            var overlaySourceSamplersUniformLocation = GL.GetUniformLocation(overlayProgram, "uSourceSamplers");
            
            GL.UseProgram(overlayProgram);
            for (var i = 0; i < MaxOverlays; i++)
                GL.Uniform1i(overlaySourceSamplersUniformLocation + i, 1, i);
            
            // Hardcode offsets and scales from 1 to MaxOverlays
            for (var i = 0; i < MaxOverlays; i++)
            {
                GL.Uniform2f(overlaySourceOffsetsUniformLocation + i, 0f, 0f);
                GL.Uniform2f(overlaySourceScalesUniformLocation + i, 1f, 1f);
            }
            
            // Create overlay sampler
            overlaySampler = GL.CreateSampler();
            GL.SamplerParameteri(overlaySampler, SamplerParameterI.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.SamplerParameteri(overlaySampler, SamplerParameterI.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.SamplerParameteri(overlaySampler, SamplerParameterI.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            GL.SamplerParameteri(overlaySampler, SamplerParameterI.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

            var borderColor = Vector4.Zero;
            GL.SamplerParameterf(overlaySampler, SamplerParameterF.TextureBorderColor, in borderColor.X);
            
            // Initialize overlay texture
            overlayTexture = GL.CreateTexture(TextureTarget.Texture2d);
            GL.TextureStorage2D(overlayTexture, 1, SizedInternalFormat.Rgba16f, size.X, size.Y);
            
            // Create overlay fbo
            overlayFramebufferHandle = GL.CreateFramebuffer();
            GL.NamedFramebufferTexture(overlayFramebufferHandle, FramebufferAttachment.ColorAttachment0, overlayTexture, 0);
            
            currentOverlaySize = size;

            // Initialize post processors
            legacyBloom = new LegacyBloom(loader);
            universalBloom = new UniversalBloom(loader);
            glitch = new Glitch(loader);
            uberPost = new UberPost(loader);
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
        logger.LogInformation("Disposing OpenGL renderer");
        
        // Unsubscribe from events
        renderingFactory.Meshes.ItemInserted -= OnMeshInserted;
        renderingFactory.Meshes.ItemRemoved -= OnMeshRemoved;
        renderingFactory.Fonts.ItemInserted -= OnFontInserted;
        renderingFactory.Fonts.ItemRemoved -= OnFontRemoved;
        renderingFactory.Texts.ItemInserted -= OnTextInserted;
        renderingFactory.Texts.ItemRemoved -= OnTextRemoved;
        
        window.MakeContextCurrent();
        
        // Delete OpenGL resources
        GL.DeleteFramebuffer(fboHandle);
        GL.DeleteRenderbuffer(fboColorBufferHandle);
        GL.DeleteRenderbuffer(fboDepthBufferHandle);
        GL.DeleteFramebuffer(postProcessFboHandle);
        GL.DeleteTexture(postProcessTextureHandle1);
        GL.DeleteTexture(postProcessTextureHandle2);
        
        GL.DeleteBuffer(vertexBufferHandle);
        GL.DeleteBuffer(indexBufferHandle);
        GL.DeleteVertexArray(vertexArrayHandle);
        
        GL.DeleteBuffer(multiDrawIndirectBufferHandle);
        GL.DeleteBuffer(multiDrawStorageBufferHandle);
        
        GL.DeleteProgram(programHandle);
        GL.DeleteSampler(fontAtlasSampler);
        
        // Delete font atlas textures
        foreach (var fontInfo in fontInfos)
        {
            if (fontInfo.AtlasTextureHandle == 0)
                continue;
            
            GL.DeleteTexture(fontInfo.AtlasTextureHandle);
        }
        
        // Delete overlay resources
        GL.DeleteProgram(overlayProgram);
        GL.DeleteSampler(overlaySampler);
        if (overlayTexture != 0)
            GL.DeleteTexture(overlayTexture);
        GL.DeleteFramebuffer(overlayFramebufferHandle);
        
        // Dispose post processors
        legacyBloom.Dispose();
        universalBloom.Dispose();
        glitch.Dispose();
        uberPost.Dispose();
    }
    
    private void OnTextInserted(object? sender, ObservableSparseSetEventArgs<Common.Text> e)
    {
        textsDirty = true;
    }

    private void OnTextRemoved(object? sender, ObservableSparseSetEventArgs<Common.Text> e)
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
        => overlayRenderers.Add(overlayRenderer);
    
    public bool RemoveOverlayRenderer(IOverlayRenderer overlayRenderer) 
        => overlayRenderers.Remove(overlayRenderer);

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
        
        var drawData = drawDataProvider.CreateDrawData();
        
        // Split draw list into opaque and transparent
        opaqueDrawCommands.Clear();
        transparentDrawCommands.Clear();

        var currentDepthInt = 0;
        foreach (ref var drawCommand in drawData.DrawCommands)
        {
            if (RenderUtil.ShouldUseTransparentDrawList(drawCommand, drawData))
            {
                transparentDrawCommands.Add(new DrawCommandWithDepth
                {
                    DrawType = drawCommand.DrawType,
                    DrawId = drawCommand.DrawId,
                    Depth = currentDepthInt / (float)(1 << 23)
                });
            }
            else
            {
                opaqueDrawCommands.Add(new DrawCommandWithDepth
                {
                    DrawType = drawCommand.DrawType,
                    DrawId = drawCommand.DrawId,
                    Depth = currentDepthInt / (float)(1 << 23)
                });
            }

            currentDepthInt++;
        }
        
        // Reverse opaque draw data list so that it is drawn
        // from back to front to avoid overdraw
        opaqueDrawCommands.Reverse();
        
        // Get camera matrix (view and projection)
        var camera = RenderUtil.GetCameraMatrix(drawData.CameraState, renderSize);
        
        // Render
        GL.Viewport(0, 0, currentFboSize.X, currentFboSize.Y);
        
        // Clear buffers
        var clearColor = drawData.ClearColor;
        var depth = 0.0f;
        GL.ClearNamedFramebufferf(fboHandle, OpenTK.Graphics.OpenGL.Buffer.Color, 0, in clearColor.R);
        GL.ClearNamedFramebufferf(fboHandle, OpenTK.Graphics.OpenGL.Buffer.Depth, 0, in depth);
        
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboHandle);
        
        // Use our program
        GL.UseProgram(programHandle);
        
        // Bind atlas texture
        for (var i = 0; i < fontInfos.Length; i++)
        {
            ref var fontInfo = ref fontInfos[i];
            if (fontInfo.AtlasTextureHandle == 0)
                continue;
            
            GL.Uniform1i(fontAtlasesUniformLocation + i, 1, i);
            GL.BindTextureUnit((uint) i, fontInfo.AtlasTextureHandle);
            GL.BindSampler((uint) i, fontAtlasSampler);
        }
        
        // Bind indirect buffer
        GL.BindBuffer(BufferTarget.DrawIndirectBuffer, multiDrawIndirectBufferHandle);
        
        // Bind storage buffers
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, multiDrawStorageBufferHandle);
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, glyphStorageBufferHandle);
        
        // Bind our vertex array
        GL.BindVertexArray(vertexArrayHandle);
        
        // Set depth function
        GL.DepthFunc(DepthFunction.Greater);
        
        // Opaque pass, disable blending, enable depth testing
        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.DepthTest);
        
        // Render opaque draw data
        RenderDrawDataList(
            CollectionsMarshal.AsSpan(opaqueDrawCommands),
            drawData,
            camera);
        
        // Transparent pass, enable blending, disable depth write
        GL.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
        GL.Enable(EnableCap.Blend);
        GL.DepthMask(false);
        
        // Render transparent draw data
        RenderDrawDataList(
            CollectionsMarshal.AsSpan(transparentDrawCommands),
            drawData,
            camera);
        
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
        var finalTexture = HandlePostProcessing(drawData.PostProcessingState, postProcessTextureHandle1, postProcessTextureHandle2);
        
        // Render overlays into final texture
        {
            // Update final texture if size changed
            if (currentOverlaySize != size)
            {
                currentOverlaySize = size;
                
                GL.DeleteTexture(overlayTexture);
                
                overlayTexture = GL.CreateTexture(TextureTarget.Texture2d);
                GL.TextureStorage2D(overlayTexture, 1, SizedInternalFormat.Rgba16f, size.X, size.Y);
            }
            
            Span<int> overlayTextures = stackalloc int[MaxOverlays];
            overlayTextures[0] = finalTexture;

            var overlayCount = 1;
            foreach (var overlayRenderer in overlayRenderers)
            {
                if (overlayCount >= MaxOverlays)
                    break;
                
                var texture = overlayRenderer.ProcessFrame(size);
                if (texture != 0)
                    overlayTextures[overlayCount++] = texture;
            }

            GL.BindImageTexture(0, overlayTexture, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba16f);
            
            // Set overlay textures and samplers
            for (var i = 0; i < overlayCount; i++)
            {
                GL.BindTextureUnit((uint) i, overlayTextures[i]);
                GL.BindSampler((uint) i, overlaySampler);
            }
            
            GL.UseProgram(overlayProgram);
            
            // Set scale and offset of first overlay
            var offsetXInt = (size.X - renderSize.X) / 2;
            var offsetYInt = (size.Y - renderSize.Y) / 2;
            var offsetX = offsetXInt / (float) size.X;
            var offsetY = offsetYInt / (float) size.Y;
            
            var scaleX = renderSize.X / (float) size.X;
            var scaleY = renderSize.Y / (float) size.Y;
            
            GL.Uniform2f(overlaySourceOffsetsUniformLocation, offsetX, offsetY);
            GL.Uniform2f(overlaySourceScalesUniformLocation, scaleX, scaleY);
            
            // Set overlay count
            GL.Uniform1i(overlaySourceCountUniformLocation, overlayCount);
            
            GL.DispatchCompute(
                (uint)MathUtil.DivideCeil(size.X, 8),
                (uint)MathUtil.DivideCeil(size.Y, 8),
                1);
            GL.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
        }
        
        // Bind overlay texture to overlay fbo
        GL.NamedFramebufferTexture(overlayFramebufferHandle, FramebufferAttachment.ColorAttachment0, overlayTexture, 0);
        
        // Present to window
        window.Present(overlayFramebufferHandle, Vector4.Zero, size, Vector2i.Zero);
    }

    private void RenderDrawDataList(in Span<DrawCommandWithDepth> drawCommands, in DrawData drawData, in Matrix3x2 camera)
    {
        if (drawCommands.Length == 0)
            return;
        
        multiDrawIndirectBuffer.Clear();
        multiDrawStorageBuffer.Clear();
        
        var meshInfosSpan = CollectionsMarshal.AsSpan(meshInfos);
        var textInfosSpan = CollectionsMarshal.AsSpan(textInfos);
        
        // Append data
        foreach (ref var drawCommand in drawCommands)
        {
            switch (drawCommand.DrawType)
            {
                case DrawType.Mesh:
                {
                    ref var meshDrawItem = ref drawData.MeshDrawItems[drawCommand.DrawId];
                    ref var meshInfo = ref meshInfosSpan[meshDrawItem.MeshHandle.Id];
                    
                    var mvp = meshDrawItem.Transform * camera;
                    
                    multiDrawStorageBuffer.Append(new MultiDrawItem
                    {
                        Mvp = mvp,
                        Color1 = meshDrawItem.Color1,
                        Color2 = meshDrawItem.Color2,
                        Z = drawCommand.Depth,
                        RenderMode = (int) meshDrawItem.RenderMode,
                        RenderType = 0, // 0 is mesh
                        GlyphOffset = 0,
                        GradientRotation = meshDrawItem.GradientRotation,
                        GradientScale = meshDrawItem.GradientScale
                    });
                    
                    multiDrawIndirectBuffer.Append(new DrawElementsIndirectCommand
                    {
                        Count = meshInfo.IndexCount,
                        InstanceCount = 1,
                        FirstIndex = meshInfo.IndexOffset,
                        BaseVertex = meshInfo.VertexOffset,
                        BaseInstance = 0
                    });
                    break;
                }
                case DrawType.Text:
                {
                    ref var textDrawItem = ref drawData.TextDrawItems[drawCommand.DrawId];
                    ref var textInfo = ref textInfosSpan[textDrawItem.TextHandle.Id];
                    
                    var mvp = textDrawItem.Transform * camera;
                    
                    multiDrawStorageBuffer.Append(new MultiDrawItem
                    {
                        Mvp = mvp,
                        Color1 = textDrawItem.Color,
                        Z = drawCommand.Depth,
                        RenderMode = 0, // 0 is normal
                        RenderType = 1, // 1 is text
                        GlyphOffset = textInfo.GlyphOffset
                    });
                    
                    multiDrawIndirectBuffer.Append(new DrawElementsIndirectCommand
                    {
                        Count = baseFontIndexCount,
                        InstanceCount = textInfo.GlyphCount,
                        FirstIndex = baseFontIndexOffset,
                        BaseVertex = baseFontVertexOffset,
                        BaseInstance = 0
                    });
                    break;
                }
                default:
                    throw new InvalidOperationException($"Unsupported or invalid data for render type '{drawCommand.DrawType}'");
            }
        }
        
        // Upload buffers to GPU
        var multiDrawIndirectBufferData = multiDrawIndirectBuffer.DataAsBytes;
        var multiDrawStorageBufferData = multiDrawStorageBuffer.DataAsBytes;
        
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

        // Draw
        GL.MultiDrawElementsIndirect(
            PrimitiveType.Triangles,
            DrawElementsType.UnsignedInt,
            IntPtr.Zero,
            multiDrawIndirectBuffer.Length,
            0);
    }

    private int HandlePostProcessing(PostProcessingState state, int texture1, int texture2)
    {
        if (legacyBloom.Process(currentFboSize, state.LegacyBloom.Intensity, state.LegacyBloom.Diffusion, state.LegacyBloom.Color, texture1, texture2))
            Swap(ref texture1, ref texture2);
        
        if (universalBloom.Process(currentFboSize, state.UniversalBloom.Intensity, state.UniversalBloom.Diffusion, state.UniversalBloom.Color, texture1, texture2))
            Swap(ref texture1, ref texture2);
        
        if (glitch.Process(currentFboSize, state.Time, state.Glitch.Speed, state.Glitch.Intensity, state.Glitch.Amount, state.Glitch.StretchMultiplier, texture1, texture2))
            Swap(ref texture1, ref texture2);
        
        if (uberPost.Process(
                currentFboSize,
                state.HueShift.Angle,
                state.LensDistortion.Intensity, state.LensDistortion.Center,
                state.ChromaticAberration.Intensity,
                state.Vignette.Center, state.Vignette.Intensity, state.Vignette.Rounded, state.Vignette.Roundness, state.Vignette.Smoothness, state.Vignette.Color,
                state.Gradient.Color1, state.Gradient.Color2, state.Gradient.Intensity, state.Gradient.Rotation, state.Gradient.Mode,
                texture1, texture2))
            Swap(ref texture1, ref texture2);
        
        return texture1;
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
        
        // Append base font quad data
        vertexBuffer.Append([
            new Vector2(0.0f, 1.0f),
            new Vector2(1.0f, 1.0f),
            new Vector2(0.0f, 0.0f),
            new Vector2(1.0f, 0.0f),
        ]);
        
        indexBuffer.Append([
            0, 1, 2,
            3, 2, 1,
        ]);
        
        // Rebuild mesh buffer
        if (renderingFactory.Meshes.Count > 0)
        {
            var maxId = renderingFactory.Meshes.Select(x => x.Key).Max();
            meshInfos.EnsureCount(maxId + 1);
        
            var meshInfosSpan = CollectionsMarshal.AsSpan(meshInfos);
            foreach (var (id, mesh) in renderingFactory.Meshes)
            {
                ref var meshInfo = ref meshInfosSpan[id];
                meshInfo.VertexOffset = vertexBuffer.Length;
                meshInfo.IndexOffset = indexBuffer.Length;
                meshInfo.IndexCount = mesh.Indices.Length;
            
                vertexBuffer.Append(mesh.Vertices);
                indexBuffer.Append(mesh.Indices);
            }

            // Update our buffers with the new data
            GL.NamedBufferData(vertexBufferHandle, vertexBuffer.LengthInBytes, vertexBuffer.Data, VertexBufferObjectUsage.DynamicDraw);
            GL.NamedBufferData(indexBufferHandle, indexBuffer.LengthInBytes, indexBuffer.Data, VertexBufferObjectUsage.DynamicDraw);
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
                var atlasHandle = GL.CreateTexture(TextureTarget.Texture2d);
                GL.TextureStorage2D(atlasHandle, 1, SizedInternalFormat.Rgb16f, font.Width, font.Height);
                GL.TextureSubImage2D(atlasHandle, 0, 0, 0, font.Width, font.Height, PixelFormat.Rgb, PixelType.HalfFloat, font.Atlas);
            
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
            GL.NamedBufferData(glyphStorageBufferHandle, glyphBuffer.LengthInBytes, glyphBuffer.Data, VertexBufferObjectUsage.DynamicDraw);
        }
        
        logger.LogInformation("Text buffer updated, registered {GlyphCount} glyphs", glyphBuffer.Length);
    }

    private void UpdateFboData(Vector2i size)
    {
        if (size == currentFboSize)
            return;
        
        if (size.X * size.Y == 0)
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
        
        logger.LogInformation("Framebuffer size updated, now at {Width}x{Height}", currentFboSize.X, currentFboSize.Y);
    }

    private static void Swap<T>(ref T a, ref T b)
    {
        (a, b) = (b, a);
    }
}