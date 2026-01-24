using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Numerics;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering.Common;
using ParallelAnimationSystem.Rendering.OpenGL.PostProcessing;
using ParallelAnimationSystem.Util;
using ParallelAnimationSystem.Windowing;
using ParallelAnimationSystem.Windowing.OpenGL;

namespace ParallelAnimationSystem.Rendering.OpenGL;

public class Renderer : IRenderer, IDisposable
{
    private record struct MeshInfo(int VertexOffset, int IndexOffset, int IndexCount);

    private record struct FontInfo(int AtlasHandle);
    
    private const int MaxFontsCount = 12;
    private const int MsaaSamples = 4;
    private const int MaxOverlays = 10;
    
    private readonly IOpenGLWindow window;
    
    // Rendering data
    private readonly Buffer<Vector2> vertexBuffer = new();
    private readonly Buffer<int> indexBuffer = new();
    private readonly List<MeshInfo?> meshInfos = [];

    private readonly List<FontInfo?> fontInfos = [];
    
    // Post processors
    private readonly Bloom bloom;
    private readonly UberPost uberPost;
    
    // Graphics data
    private readonly int baseFontVertexOffset, baseFontIndexOffset, baseFontIndexCount;
    
    private readonly int vertexArrayHandle, vertexBufferHandle, indexBufferHandle;
    private readonly int multiDrawIndirectBufferHandle;
    private int multiDrawIndirectBufferSize;
    private readonly int multiDrawStorageBufferHandle;
    private int multiDrawStorageBufferSize;
    private readonly int multiDrawGlyphBufferHandle;
    private int multiDrawGlyphBufferSize;
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

    private readonly List<DrawList.DrawData> opaqueDrawData = [];
    private readonly List<DrawList.DrawData> transparentDrawData = [];
    
    private readonly Buffer<DrawElementsIndirectCommand> multiDrawIndirectBuffer = new();
    private readonly Buffer<MultiDrawItem> multiDrawStorageBuffer = new();
    private readonly Buffer<RenderGlyph> multiDrawGlyphBuffer = new();
    
    // Overlay renderers
    private readonly List<IOverlayRenderer> overlayRenderers = [];

    // Injected dependencies
    private readonly AppSettings appSettings;
    private readonly IncomingResourceQueue incomingResourceQueue;
    private readonly ILogger<Renderer> logger;

    public Renderer(
        AppSettings appSettings,
        IWindow window,
        ResourceLoader loader,
        IncomingResourceQueue incomingResourceQueue,
        ILogger<Renderer> logger)
    {
        this.appSettings = appSettings;
        this.window = (IOpenGLWindow) window;
        this.incomingResourceQueue = incomingResourceQueue;
        this.logger = logger;
        
        logger.LogInformation("Initializing OpenGL renderer");
        
        this.window.MakeContextCurrent();
        
        // Load OpenGL bindings
        GLLoader.LoadBindings(new BindingsContext(this.window));
        
        logger.LogInformation("Window created");
        
        // Enable multisampling
        GL.Enable(EnableCap.Multisample);
        
        // Log OpenGL info
        logger.LogInformation("OpenGL: {Version}", GL.GetString(StringName.Version));
        logger.LogInformation("Renderer: {Renderer}", GL.GetString(StringName.Renderer));
        logger.LogInformation("Vendor: {Vendor}", GL.GetString(StringName.Vendor));
        logger.LogInformation("Shading language: {ShadingLanguage}", GL.GetString(StringName.ShadingLanguageVersion));

        #region Engine Resource Initialization

        // Create text mesh (quad)
        baseFontVertexOffset = 0;
        baseFontIndexOffset = 0;
        baseFontIndexCount = 6;
        
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

        #endregion
        
        #region OpenGL Data Initialization

        {
            var size = window.FramebufferSize;
            
            // We will exclusively use DSA for this project
            vertexArrayHandle = GL.CreateVertexArray();
            vertexBufferHandle = GL.CreateBuffer();
            indexBufferHandle = GL.CreateBuffer();
            
            // Upload initial mesh data
            var vertexBufferData = vertexBuffer.Data;
            var indexBufferData = indexBuffer.Data;
            GL.NamedBufferData(vertexBufferHandle, vertexBufferData.Length, vertexBufferData, VertexBufferObjectUsage.DynamicDraw);
            GL.NamedBufferData(indexBufferHandle, indexBufferData.Length, indexBufferData, VertexBufferObjectUsage.DynamicDraw);

            // Bind buffers to vertex array
            GL.EnableVertexArrayAttrib(vertexArrayHandle, 0);
            GL.VertexArrayVertexBuffer(vertexArrayHandle, 0, vertexBufferHandle, IntPtr.Zero, Unsafe.SizeOf<Vector2>());
            GL.VertexArrayAttribFormat(vertexArrayHandle, 0, 2, VertexAttribType.Float, false, 0);
            GL.VertexArrayAttribBinding(vertexArrayHandle, 0, 0);

            GL.VertexArrayElementBuffer(vertexArrayHandle, indexBufferHandle);

            // Initialize multi draw buffer
            multiDrawIndirectBufferHandle = GL.CreateBuffer();
            multiDrawStorageBufferHandle = GL.CreateBuffer();
            multiDrawGlyphBufferHandle = GL.CreateBuffer();

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
            uberPost = new UberPost(loader);
            bloom = new Bloom(loader);
        }

        #endregion
    }
    
    public void Dispose()
    {
        // Don't dispose because it will die with the context anyway
        // TODO: If we ever need to reinit just the renderer without disposing the window, implement this
        
        // logger.LogInformation("Disposing OpenGL renderer");
        //
        // // Delete OpenGL resources
        // GL.DeleteFramebuffer(fboHandle);
        // GL.DeleteRenderbuffer(fboColorBufferHandle);
        // GL.DeleteRenderbuffer(fboDepthBufferHandle);
        // GL.DeleteFramebuffer(postProcessFboHandle);
        // GL.DeleteTexture(postProcessTextureHandle1);
        // GL.DeleteTexture(postProcessTextureHandle2);
        //
        // GL.DeleteBuffer(vertexBufferHandle);
        // GL.DeleteBuffer(indexBufferHandle);
        // GL.DeleteVertexArray(vertexArrayHandle);
        //
        // GL.DeleteBuffer(multiDrawIndirectBufferHandle);
        // GL.DeleteBuffer(multiDrawStorageBufferHandle);
        // GL.DeleteBuffer(multiDrawGlyphBufferHandle);
        //
        // GL.DeleteProgram(programHandle);
        // GL.DeleteSampler(fontAtlasSampler);
        //
        // // Delete font atlas textures
        // foreach (var fontInfoNullable in fontInfos)
        // {
        //     if (!fontInfoNullable.HasValue)
        //         continue;
        //     
        //     var fontInfo = fontInfoNullable.Value;
        //     GL.DeleteTexture(fontInfo.AtlasHandle);
        // }
        // 
        // // Delete overlay resources
        // GL.DeleteProgram(overlayProgram);
        // GL.DeleteSampler(overlaySampler);
        // if (overlayTexture != 0)
        //     GL.DeleteTexture(overlayTexture);
        // GL.DeleteFramebuffer(overlayFramebufferHandle);
        //
        // // Dispose post processors
        // uberPost.Dispose();
        // bloom.Dispose();
    }
    
    public void AddOverlayRenderer(IOverlayRenderer overlayRenderer)
        => overlayRenderers.Add(overlayRenderer);
    
    public bool RemoveOverlayRenderer(IOverlayRenderer overlayRenderer) 
        => overlayRenderers.Remove(overlayRenderer);

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
        
        // Get camera matrix (view and projection)
        var camera = RenderUtil.GetCameraMatrix(oglDrawList.CameraData, renderSize);
        
        // Render
        GL.Viewport(0, 0, currentFboSize.X, currentFboSize.Y);
        
        // Clear buffers
        var clearColor = oglDrawList.ClearColor.ToVector();
        var depth = 0.0f;
        GL.ClearNamedFramebufferf(fboHandle, OpenTK.Graphics.OpenGL.Buffer.Color, 0, in clearColor.X);
        GL.ClearNamedFramebufferf(fboHandle, OpenTK.Graphics.OpenGL.Buffer.Depth, 0, in depth);
        
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboHandle);
        
        // Use our program
        GL.UseProgram(programHandle);
        
        // Bind atlas texture
        for (var i = 0; i < fontInfos.Count; i++)
        {
            var fontInfoNullable = fontInfos[i];
            if (!fontInfoNullable.HasValue)
                continue;
            
            var fontInfo = fontInfoNullable.Value;
            GL.Uniform1i(fontAtlasesUniformLocation + i, 1, i);
            GL.BindTextureUnit((uint) i, fontInfo.AtlasHandle);
            GL.BindSampler((uint) i, fontAtlasSampler);
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
        GL.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
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
        var finalTexture = HandlePostProcessing(oglDrawList.PostProcessingData, postProcessTextureHandle1, postProcessTextureHandle2);
        
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

    private void RenderDrawDataList(List<DrawList.DrawData> drawDataList, Matrix3x2 camera)
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
            var transform = drawData.Transform;
            var color1 = drawData.Color1;
            var color2 = drawData.Color2;
            var renderMode = drawData.RenderMode;
            
            var mvp = transform * camera;
            
            multiDrawStorageBuffer.Append(new MultiDrawItem
            {
                Mvp = mvp,
                Color1 = color1.ToVector(),
                Color2 = color2.ToVector(),
                Z = RenderUtil.EncodeIntDepth(drawData.Index),
                RenderMode = (int) renderMode,
                RenderType = (int) renderType,
                GlyphOffset = multiDrawGlyphBuffer.Length,
            });

            switch (renderType)
            {
                case RenderType.Mesh:
                    var mesh = drawData.Mesh;
                    Debug.Assert(mesh is not null);
                    
                    var meshInfoNullable = meshInfos[mesh.Id];
                    Debug.Assert(meshInfoNullable.HasValue);
                    
                    var meshInfo = meshInfoNullable.Value;
                    
                    multiDrawIndirectBuffer.Append(new DrawElementsIndirectCommand
                    {
                        Count = meshInfo.IndexCount,
                        InstanceCount = 1,
                        FirstIndex = meshInfo.IndexOffset,
                        BaseVertex = meshInfo.VertexOffset,
                        BaseInstance = 0
                    });
                    break;
                case RenderType.Text:
                    var text = drawData.Text;
                    Debug.Assert(text is not null);
                    
                    multiDrawIndirectBuffer.Append(new DrawElementsIndirectCommand
                    {
                        Count = baseFontIndexCount,
                        InstanceCount = text.Glyphs.Length,
                        FirstIndex = baseFontIndexOffset,
                        BaseVertex = baseFontVertexOffset,
                        BaseInstance = 0
                    });
                    multiDrawGlyphBuffer.Append(text.Glyphs);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported or invalid data for render type '{renderType}'");
            }
        }
        
        // Upload buffers to GPU
        var multiDrawIndirectBufferData = multiDrawIndirectBuffer.DataAsBytes;
        var multiDrawStorageBufferData = multiDrawStorageBuffer.DataAsBytes;
        var multiDrawGlyphBufferData = multiDrawGlyphBuffer.DataAsBytes;
        
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
            multiDrawIndirectBuffer.Length,
            0);
    }

    private int HandlePostProcessing(PostProcessingData data, int texture1, int texture2)
    {
        if (bloom.Process(currentFboSize, data.Bloom.Intensity, data.Bloom.Diffusion, texture1, texture2))
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

    private void UpdateOpenGlData(Vector2i size)
    {
        UpdateMeshData();
        UpdateFontData();
        UpdateFboData(size);
    }

    private void UpdateMeshData()
    {
        if (!incomingResourceQueue.TryDequeueAllMeshes(out var incomingMeshes))
            return;

        foreach (var incomingMesh in incomingMeshes)
        {
            meshInfos.EnsureCount(incomingMesh.MeshId);
            
            var vertexOffset = vertexBuffer.Length;
            var indexOffset = indexBuffer.Length;
            var indexCount = incomingMesh.Indices.Length;
            meshInfos.Add(new MeshInfo(vertexOffset, indexOffset, indexCount));
            
            vertexBuffer.Append(incomingMesh.Vertices);
            indexBuffer.Append(incomingMesh.Indices);
        }

        // Update our buffers with the new data
        GL.NamedBufferData(vertexBufferHandle, vertexBuffer.LengthInBytes, vertexBuffer.Data, VertexBufferObjectUsage.DynamicDraw);
        GL.NamedBufferData(indexBufferHandle, indexBuffer.LengthInBytes, indexBuffer.Data, VertexBufferObjectUsage.DynamicDraw);
        
        logger.LogInformation("OpenGL mesh buffers updated, now at {VertexCount} vertices and {IndexCount} indices", 
            vertexBuffer.Length,
            indexBuffer.Length);
    }
    
    private void UpdateFontData()
    {
        if (!incomingResourceQueue.TryDequeueAllFonts(out var incomingFonts))
            return;
        
        foreach (var font in incomingFonts)
        {
            if (font.FontId >= MaxFontsCount)
                throw new InvalidOperationException($"Exceeded maximum number of fonts ({MaxFontsCount}) supported by the renderer");
            
            var atlas = font.Atlas;
            
            GL.PixelStorei(PixelStoreParameter.UnpackAlignment, 1);
            
            // Create texture
            var atlasHandle = GL.CreateTexture(TextureTarget.Texture2d);
            GL.TextureStorage2D(atlasHandle, 1, SizedInternalFormat.Rgb16f, atlas.Width, atlas.Height);
            GL.TextureSubImage2D(atlasHandle, 0, 0, 0, atlas.Width, atlas.Height, PixelFormat.Rgb, PixelType.HalfFloat, atlas.Data);
            
            // Put it in our font info list
            fontInfos.EnsureCount(font.FontId + 1);
            fontInfos[font.FontId] = new FontInfo(atlasHandle);
        }
        
        logger.LogInformation("Updated {FontCount} font atlases", incomingFonts.Count);
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
        
        logger.LogInformation("OpenGL framebuffer size updated, now at {Width}x{Height}", currentFboSize.X, currentFboSize.Y);
    }

    private static void Swap<T>(ref T a, ref T b)
    {
        (a, b) = (b, a);
    }
}