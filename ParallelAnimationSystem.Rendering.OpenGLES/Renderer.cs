using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGLES2;
using System.Numerics;
using System.Runtime.InteropServices;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Platform.OpenGL;
using ParallelAnimationSystem.Rendering.Common;
using ParallelAnimationSystem.Rendering.Data;
using ParallelAnimationSystem.Rendering.OpenGLES.PostProcessing;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Rendering.OpenGLES;

public class Renderer : IRenderer, IDisposable
{
    private const int MsaaSamples = 4;
    private const int MaxOverlays = 10;
    
    private const int FontTextureWidth = 2048;

    private struct MeshInfo
    {
        public int IndexOffset;
        public int IndexCount;
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
    
    private readonly IOpenGLSurface surface;
    
    // Rendering data
    private readonly Buffer<Vector2> vertexBuffer = new();
    private readonly Buffer<int> indexBuffer = new();
    private readonly Buffer<RenderGlyph> glyphBuffer = new();
    
    private readonly List<MeshInfo> meshInfos = [];
    private readonly List<TextInfo> textInfos = [];

    private readonly int programHandle;
    private readonly int
        mvpUniformLocation,
        zUniformLocation,
        gradientRotationUniformLocation,
        gradientScaleUniformLocation,
        renderModeUniformLocation,
        color1UniformLocation,
        color2UniformLocation;
    private readonly int glyphProgramHandle;
    private readonly int glyphMvpUniformLocation, glyphZUniformLocation, glyphBaseColorUniformLocation;
    private readonly int glyphViewportSizeUniformLocation;

    // TMPX global font data as texture buffers (populated by UpdateFontData)
    private int fontCurvesTextureHandle;
    private int fontCurveIndicesTextureHandle;
    private int fontBandEntriesTextureHandle;
    private int fontShapeEntriesTextureHandle;

    private Vector2i currentFboSize;
    private int fboColorBufferHandle, fboDepthBufferHandle;
    private readonly int fboHandle;
    private int postProcessTextureHandle1, postProcessTextureHandle2;
    private readonly int postProcessFboHandle;

    private readonly int emptyVao;
    
    // Post-processing
    private readonly LegacyBloom legacyBloom;
    private readonly UniversalBloom universalBloom;
    private readonly Glitch glitch;
    private readonly Grain grain;
    private readonly UberPost uberPost;
    
    // Temporary draw data lists
    private readonly List<DrawCommandWithDepth> opaqueDrawCommands = [];
    private readonly List<DrawCommandWithDepth> transparentDrawCommands = [];
    
    private int mainVertexArrayHandle, mainVertexBufferHandle, mainIndexBufferHandle;

    private readonly int textVertexArrayHandle;
    private int textInstanceBufferHandle;

    // Dirty flags
    private bool meshBufferDirty = true;
    private bool textsDirty = true;
    private bool fontBuffersDirty = true;
    
    // Injected dependencies
    private readonly RenderingFactory renderingFactory;
    private readonly ILogger<Renderer> logger;
    
    public Renderer(
        IRenderingFactory renderingFactory,
        IOpenGLSurface surface,
        ResourceLoader loader,
        ILogger<Renderer> logger)
    {
        this.renderingFactory = (RenderingFactory) renderingFactory;
        this.surface = surface;
        this.logger = logger;
        
        logger.LogInformation("Initializing OpenGL ES renderer");
        
        // Log OpenGL information
        logger.LogInformation("OpenGL ES: {Version}", GL.GetString(StringName.Version));
        logger.LogInformation("Renderer: {Renderer}", GL.GetString(StringName.Renderer));
        logger.LogInformation("Vendor: {Vendor}", GL.GetString(StringName.Vendor));
        logger.LogInformation("Shading language: {ShadingLanguage}", GL.GetString(StringName.ShadingLanguageVersion));
        
        #region OpenGL Data Initialization

        {
            var initialSize = this.surface.RenderSize;
            
            // Create main program handle
            programHandle = LoaderUtil.LoadShaderProgram(loader, "UnlitVertex", "UnlitFragment");

            // Get uniform locations
            mvpUniformLocation = GL.GetUniformLocation(programHandle, "uMvp");
            zUniformLocation = GL.GetUniformLocation(programHandle, "uZ");
            gradientRotationUniformLocation = GL.GetUniformLocation(programHandle, "uGradientRotation");
            gradientScaleUniformLocation = GL.GetUniformLocation(programHandle, "uGradientScale");
            renderModeUniformLocation = GL.GetUniformLocation(programHandle, "uRenderMode");
            color1UniformLocation = GL.GetUniformLocation(programHandle, "uColor1");
            color2UniformLocation = GL.GetUniformLocation(programHandle, "uColor2");

            // Create glyph program handle
            glyphProgramHandle = LoaderUtil.LoadShaderProgram(loader, "TextVertex", "TextFragment");

            // Get glyph uniform locations
            glyphMvpUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uMvp");
            glyphZUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uZ");
            glyphBaseColorUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uBaseColor");
            glyphViewportSizeUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uViewportSize");

            // Bind texture sampler uniforms to fixed texture units
            var curvesUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uCurves");
            var curveIndicesUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uCurveIndices");
            var bandEntriesUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uBandEntries");
            var shapeEntriesUniformLocation = GL.GetUniformLocation(glyphProgramHandle, "uShapeEntries");

            GL.UseProgram(glyphProgramHandle);
            GL.Uniform1i(curvesUniformLocation, 0);
            GL.Uniform1i(curveIndicesUniformLocation, 1);
            GL.Uniform1i(bandEntriesUniformLocation, 2);
            GL.Uniform1i(shapeEntriesUniformLocation, 3);

            // Initialize text vertex array.
            textVertexArrayHandle = GL.GenVertexArray();
            GL.BindVertexArray(textVertexArrayHandle);

            // All attributes are per-instance (divisor=1); the 6 quad vertices come from gl_VertexID.
            GL.EnableVertexAttribArray(0);  // transform col 0
            GL.EnableVertexAttribArray(1);  // transform col 1
            GL.EnableVertexAttribArray(2);  // transform col 2
            GL.EnableVertexAttribArray(3);  // color
            GL.EnableVertexAttribArray(4);  // shapeEntryIndex

            GL.VertexAttribDivisor(0, 1);
            GL.VertexAttribDivisor(1, 1);
            GL.VertexAttribDivisor(2, 1);
            GL.VertexAttribDivisor(3, 1);
            GL.VertexAttribDivisor(4, 1);

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
            glitch = new Glitch(loader, vertexShader);
            grain = new Grain(loader, vertexShader);
            uberPost = new UberPost(loader, vertexShader);

            // Clean up
            GL.DeleteShader(vertexShader);
        }

        #endregion
        
        // Subscribe to events
        this.renderingFactory.FontBuffersUpdated += OnFontBuffersUpdated;
        this.renderingFactory.Meshes.ItemInserted += OnMeshInserted;
        this.renderingFactory.Meshes.ItemRemoved += OnMeshRemoved;
        this.renderingFactory.Texts.ItemInserted += OnTextInserted;
        this.renderingFactory.Texts.ItemRemoved += OnTextRemoved;
    }
    
    public void Dispose()
    {
        logger.LogInformation("Disposing OpenGL ES renderer");
        
        // Unsubscribe from events
        renderingFactory.FontBuffersUpdated -= OnFontBuffersUpdated;
        renderingFactory.Meshes.ItemInserted -= OnMeshInserted;
        renderingFactory.Meshes.ItemRemoved -= OnMeshRemoved;
        renderingFactory.Texts.ItemInserted -= OnTextInserted;
        renderingFactory.Texts.ItemRemoved -= OnTextRemoved;
        
        // Context already lost, no need to continue disposing
        if (surface.IsContextLost)
            return;
        
        // Delete GL resources
        GL.DeleteProgram(programHandle);
        GL.DeleteProgram(glyphProgramHandle);
        
        GL.DeleteBuffer(mainVertexBufferHandle);
        GL.DeleteBuffer(mainIndexBufferHandle);
        GL.DeleteVertexArray(mainVertexArrayHandle);
        
        GL.DeleteBuffer(textInstanceBufferHandle);
        GL.DeleteVertexArray(textVertexArrayHandle);
        
        GL.DeleteTexture(fontCurvesTextureHandle);
        GL.DeleteTexture(fontCurveIndicesTextureHandle);
        GL.DeleteTexture(fontBandEntriesTextureHandle);
        GL.DeleteTexture(fontShapeEntriesTextureHandle);

        GL.DeleteRenderbuffer(fboColorBufferHandle);
        GL.DeleteRenderbuffer(fboDepthBufferHandle);
        GL.DeleteFramebuffer(fboHandle);
        
        GL.DeleteTexture(postProcessTextureHandle1);
        GL.DeleteTexture(postProcessTextureHandle2);
        GL.DeleteFramebuffer(postProcessFboHandle);
        
        GL.DeleteVertexArray(emptyVao);
        
        legacyBloom.Dispose();
        universalBloom.Dispose();
        glitch.Dispose();
        grain.Dispose();
        uberPost.Dispose();
    }

    private void OnFontBuffersUpdated(object? sender, EventArgs e)
    {
        fontBuffersDirty = true;
    }

    private void OnTextInserted(object? sender, ObservableSparseSetEventArgs<Text> e)
    {
        textsDirty = true;
    }

    private void OnTextRemoved(object? sender, ObservableSparseSetEventArgs<Text> e)
    {
        textsDirty = true;
    }
    
    private void OnMeshInserted(object? sender, ObservableSparseSetEventArgs<Mesh> e)
    {
        meshBufferDirty = true;
    }

    private void OnMeshRemoved(object? sender, ObservableSparseSetEventArgs<Mesh> e)
    {
        meshBufferDirty = true;
    }
    
    public void ProcessFrame(IDrawDataProvider drawDataProvider)
    {
        var renderSize = surface.RenderSize;
        
        // Update OpenGL data
        UpdateOpenGlData(renderSize);
        
        var drawData = drawDataProvider.CreateDrawData();
        
        // Get camera matrix (view and projection)
        var camera = RenderUtil.GetCameraMatrix(drawData.CameraState, renderSize);
        
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
        
        // Bind FBO
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboHandle);
        
        // Get rid of samplers
        GL.BindSampler(0, 0);
        GL.BindSampler(1, 0);
        GL.BindSampler(2, 0);
        GL.BindSampler(3, 0);
        
        // Bind the four TMPX texture buffers to texture units 0-3
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, fontCurvesTextureHandle);
        
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2d, fontCurveIndicesTextureHandle);
        
        GL.ActiveTexture(TextureUnit.Texture2);
        GL.BindTexture(TextureTarget.Texture2d, fontBandEntriesTextureHandle);
        
        GL.ActiveTexture(TextureUnit.Texture3);
        GL.BindTexture(TextureTarget.Texture2d, fontShapeEntriesTextureHandle);
        
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
        var finalTexture = HandlePostProcessing(drawData.PostProcessingState, postProcessTextureHandle1, postProcessTextureHandle2);
        
        // Present to window
        surface.Present(finalTexture, renderSize, new ColorRgba());
    }
    
    private int HandlePostProcessing(PostProcessingState state, int texture1, int texture2)
    {
        // Disable depth testing and blending
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.Blend);
        
        if (legacyBloom.Process(currentFboSize, state.LegacyBloom.Intensity, state.LegacyBloom.Diffusion, state.LegacyBloom.Color, texture1, texture2))
            Swap(ref texture1, ref texture2);
        
        if (universalBloom.Process(currentFboSize, state.UniversalBloom.Intensity, state.UniversalBloom.Diffusion, state.UniversalBloom.Color, texture1, texture2))
            Swap(ref texture1, ref texture2);
        
        if (glitch.Process(currentFboSize, state.Time, state.Glitch.Speed, state.Glitch.Intensity, state.Glitch.Amount, state.Glitch.StretchMultiplier, texture1, texture2))
            Swap(ref texture1, ref texture2);
        
        if (grain.Process(currentFboSize, state.Time, state.Grain.Colored, state.Grain.Intensity, state.Grain.Size, state.Grain.LuminanceContribution, texture1, texture2))
            Swap(ref texture1, ref texture2);
        
        if (uberPost.Process(
                currentFboSize,
                state.HueShift.Angle,
                state.LensDistortion.Intensity, state.LensDistortion.Center,
                state.ChromaticAberration.Intensity,
                state.Vignette.Center, state.Vignette.Intensity, state.Vignette.Rounded, state.Vignette.Roundness, state.Vignette.Smoothness, state.Vignette.Color, state.Vignette.Mode,
                state.Gradient.Color1, state.Gradient.Color2, state.Gradient.Intensity, state.Gradient.Rotation, state.Gradient.Mode,
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
                    GL.Uniform1f(gradientRotationUniformLocation, meshDrawItem.GradientRotation);
                    GL.Uniform1f(gradientScaleUniformLocation, meshDrawItem.GradientScale);
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

                    if (textInfo.GlyphCount == 0)
                        break;

                    var mvp = textDrawItem.Transform * camera;

                    GL.UseProgram(glyphProgramHandle);

                    unsafe
                    {
                        GL.UniformMatrix3x2fv(glyphMvpUniformLocation, 1, false, (float*)&mvp);
                    }
                    GL.Uniform1f(glyphZUniformLocation, drawCommand.Depth);
                    GL.Uniform4f(glyphBaseColorUniformLocation,
                        textDrawItem.Color.R, textDrawItem.Color.G,
                        textDrawItem.Color.B, textDrawItem.Color.A);
                    GL.Uniform2f(glyphViewportSizeUniformLocation, currentFboSize.X, currentFboSize.Y);
                    
                    GL.BindVertexArray(textVertexArrayHandle);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, textInstanceBufferHandle);

                    var stride = Unsafe.SizeOf<RenderGlyph>();
                    var baseOff = textInfo.GlyphOffset * stride;

                    GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, baseOff);
                    GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, baseOff + 8);
                    GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, baseOff + 16);
                    GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, stride, baseOff + 24);
                    GL.VertexAttribIPointer(4, 1, VertexAttribIType.Int, stride, baseOff + 40);
                    
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
        if (!fontBuffersDirty)
            return;
        
        fontBuffersDirty = false;
        
        // Upload curves buffer
        if (fontCurvesTextureHandle != 0)
            GL.DeleteTexture(fontCurvesTextureHandle);
        
        if (renderingFactory.FontCurves.Length > 0)
        {
            fontCurvesTextureHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2d, fontCurvesTextureHandle);
            
            // each curve takes exactly 3 texels
            var texWidth = FontTextureWidth;
            var texHeight = (renderingFactory.FontCurves.Length * 3 + FontTextureWidth - 1) / FontTextureWidth; // Round up division
            GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rg32f, texWidth, texHeight);
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            var bufSize = texWidth * texHeight * 2 * sizeof(float);
            var buf = new byte[bufSize];
            var fontCurvesSpan = MemoryMarshal.AsBytes(renderingFactory.FontCurves);
            fontCurvesSpan.CopyTo(buf);
            
            GL.TexSubImage2D(TextureTarget.Texture2d, 0, 0, 0, texWidth, texHeight, PixelFormat.Rg, PixelType.Float, buf); 
        }
        
        // Upload curve indices buffer
        if (fontCurveIndicesTextureHandle != 0)
            GL.DeleteTexture(fontCurveIndicesTextureHandle);

        if (renderingFactory.FontCurveIndices.Length > 0)
        {
            fontCurveIndicesTextureHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2d, fontCurveIndicesTextureHandle);

            // each curve index takes exactly 1 texel
            var texWidth = FontTextureWidth;
            var texHeight = (renderingFactory.FontCurveIndices.Length + FontTextureWidth - 1) / FontTextureWidth; // Round up division
            GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.R32i, texWidth, texHeight);
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            
            var bufSize = texWidth * texHeight * sizeof(int);
            var buf = new byte[bufSize];
            var fontCurveIndicesSpan = MemoryMarshal.AsBytes(renderingFactory.FontCurveIndices);
            fontCurveIndicesSpan.CopyTo(buf);
            
            GL.TexSubImage2D(TextureTarget.Texture2d, 0, 0, 0, texWidth, texHeight, PixelFormat.RedInteger, PixelType.Int, buf);
        }
        
        // Upload band entries buffer
        if (fontBandEntriesTextureHandle != 0)
            GL.DeleteTexture(fontBandEntriesTextureHandle);
        
        if (renderingFactory.FontBandEntries.Length > 0)
        {
            fontBandEntriesTextureHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2d, fontBandEntriesTextureHandle);
            
            // each band entry takes exactly 1 texel
            var texWidth = FontTextureWidth;
            var texHeight = (renderingFactory.FontBandEntries.Length + FontTextureWidth - 1) / FontTextureWidth; // Round up division
            GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rg32i, texWidth, texHeight);
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            
            var bufSize = texWidth * texHeight * 2 * sizeof(int);
            var buf = new byte[bufSize];
            var fontBandEntriesSpan = MemoryMarshal.AsBytes(renderingFactory.FontBandEntries);
            fontBandEntriesSpan.CopyTo(buf);
            
            GL.TexSubImage2D(TextureTarget.Texture2d, 0, 0, 0, texWidth, texHeight, PixelFormat.RgInteger, PixelType.Int, buf);
        }
        
        // Upload shape entries buffer
        if (fontShapeEntriesTextureHandle != 0)
            GL.DeleteTexture(fontShapeEntriesTextureHandle);

        if (renderingFactory.FontShapeEntries.Length > 0)
        {
            fontShapeEntriesTextureHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2d, fontShapeEntriesTextureHandle);

            // each shape entry takes exactly 4 texel
            var texWidth = FontTextureWidth;
            var texHeight = (renderingFactory.FontShapeEntries.Length * 4 + FontTextureWidth - 1) / FontTextureWidth; // Round up division
            GL.TexStorage2D(TextureTarget.Texture2d, 1, SizedInternalFormat.Rgba32ui, texWidth, texHeight);
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            
            var gpuShapeEntries = renderingFactory.FontShapeEntries.Select(x => new GpuShapeEntry
            {
                HorizontalBandEntryBaseIndex = x.HorizontalBandEntryBaseIndex,
                HorizontalBandEntryCount = x.HorizontalBandEntryCount,
                HorizontalBandScale = x.HorizontalBandScale,
                HorizontalBandOffset = x.HorizontalBandOffset,
                VerticalBandEntryBaseIndex = x.VerticalBandEntryBaseIndex,
                VerticalBandEntryCount = x.VerticalBandEntryCount,
                VerticalBandScale = x.VerticalBandScale,
                VerticalBandOffset = x.VerticalBandOffset,
                Min = x.Min,
                Max = x.Max,
                Color = x.Color
            }).ToArray();
            
            var bufSize = texWidth * texHeight * 4 * sizeof(uint);
            var buf = new byte[bufSize];
            var fontShapeEntriesSpan = MemoryMarshal.AsBytes(gpuShapeEntries);
            fontShapeEntriesSpan.CopyTo(buf);
            
            GL.TexSubImage2D(TextureTarget.Texture2d, 0, 0, 0, texWidth, texHeight, PixelFormat.RgbaInteger, PixelType.UnsignedInt, buf);
        }

        logger.LogInformation(
            "Font buffers updated, registered {Curves} curves, {CurveIndices} curve indices, {Bands} band entries, {Shapes} shape entries",
            renderingFactory.FontCurves.Length,
            renderingFactory.FontCurveIndices.Length,
            renderingFactory.FontBandEntries.Length,
            renderingFactory.FontShapeEntries.Length);
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