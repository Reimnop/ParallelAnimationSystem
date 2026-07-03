using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics.OpenGL;
using System.Numerics;
using System.Runtime.InteropServices;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Platform.OpenGL;
using ParallelAnimationSystem.Rendering.Common;
using ParallelAnimationSystem.Rendering.Data;
using ParallelAnimationSystem.Rendering.OpenGL.PostProcessing;
using ParallelAnimationSystem.Util;
using BandEntry = Tmpx.Common.BandEntry;
using QuadraticCurve = Tmpx.Common.QuadraticCurve;

namespace ParallelAnimationSystem.Rendering.OpenGL;

public class Renderer : IRenderer, IDisposable
{
    private enum LifecycleCommandType
    {
        Create,
        Destroy
    }
    
    private readonly record struct MeshLifecycleCommand(int Id, LifecycleCommandType Type, (Vector2[] Vertices, int[] Indices)? Data = null);
    private readonly record struct TextLifecycleCommand(int Id, LifecycleCommandType Type, RenderGlyph[]? Data = null);
    
    private struct MeshInfo
    {
        public Allocation VertexBufferAllocation;
        public Allocation IndexBufferAllocation;
        public int VertexCount; // not currently used
        public int IndexCount;
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
    
    private const int MsaaSamples = 4;
    private const int MaxOverlays = 10;

    // SSBO binding points (must match shader layout(binding=N))
    private const int BindMultiDraw = 0;
    private const int BindGlyphs = 1;
    private const int BindCurves = 2;
    private const int BindCurveIndices = 3;
    private const int BindBandEntries = 4;
    private const int BindShapeEntries = 5;
    
    // Mesh rendering data
    private const int InitialVertexBufferCapacity = 1024;
    private const int InitialIndexBufferCapacity = 1024;

    private readonly PooledSuballocator vertexBufferAllocator = new(InitialVertexBufferCapacity);
    private readonly PooledSuballocator indexBufferAllocator = new(InitialIndexBufferCapacity);

    private readonly List<MeshInfo> meshInfos = [];
    
    // Text rendering data
    private const int InitialGlyphBufferCapacity = 1024;
    
    private readonly PooledSuballocator glyphBufferAllocator = new(InitialVertexBufferCapacity);
    private readonly List<TextInfo> textInfos = [];
    
    // Post processors
    private readonly LegacyBloom legacyBloom;
    private readonly UniversalBloom universalBloom;
    private readonly Glitch glitch;
    private readonly Grain grain;
    private readonly UberPost uberPost;
    
    // Graphics data
    private readonly int vertexArrayHandle;
    private int vertexBufferHandle, indexBufferHandle;
    private int glyphStorageBufferHandle;
    
    private readonly int multiDrawIndirectBufferHandle;
    private int multiDrawIndirectBufferSize;
    private readonly int multiDrawStorageBufferHandle;
    private int multiDrawStorageBufferSize;
    private readonly int programHandle;

    // TMPX SSBO handles, populated in UpdateFontData
    private readonly int curveSsboHandle;
    private readonly int curveIndexSsboHandle;
    private readonly int bandEntrySsboHandle;
    private readonly int shapeEntrySsboHandle;
    private readonly int viewportSizeUniformLocation;
    
    private Vector2i currentFboSize;
    private int fboColorBufferHandle, fboDepthBufferHandle;
    private readonly int fboHandle;
    private int postProcessTextureHandle1, postProcessTextureHandle2;
    private readonly int postProcessFboHandle;

    private readonly List<DrawCommandWithDepth> opaqueDrawCommands = [];
    private readonly List<DrawCommandWithDepth> transparentDrawCommands = [];
    
    private readonly Buffer<DrawElementsIndirectCommand> multiDrawIndirectBuffer = new();
    private readonly Buffer<MultiDrawItem> multiDrawStorageBuffer = new();
    
    // Lifecycle command lists
    private readonly List<MeshLifecycleCommand> meshLifecycleCommands = [];
    private readonly List<TextLifecycleCommand> textLifecycleCommands = [];
    
    // Dirty flags
    private bool fontBuffersDirty = true;

    // Injected dependencies
    private readonly RenderingFactory renderingFactory;
    private readonly IOpenGLSurface surface;
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
        
        logger.LogInformation("Initializing OpenGL renderer");
        
        // Enable multisampling
        GL.Enable(EnableCap.Multisample);
        
        // Log OpenGL info
        logger.LogInformation("OpenGL: {Version}", GL.GetString(StringName.Version));
        logger.LogInformation("Renderer: {Renderer}", GL.GetString(StringName.Renderer));
        logger.LogInformation("Vendor: {Vendor}", GL.GetString(StringName.Vendor));
        logger.LogInformation("Shading language: {ShadingLanguage}", GL.GetString(StringName.ShadingLanguageVersion));
        
        #region OpenGL Data Initialization

        {
            var size = surface.RenderSize;
            
            // Create vertex array and buffers
            vertexArrayHandle = GL.CreateVertexArray();
            
            vertexBufferHandle = GL.CreateBuffer();
            GL.NamedBufferData(vertexBufferHandle, InitialVertexBufferCapacity * Unsafe.SizeOf<Vector2>(), IntPtr.Zero, VertexBufferObjectUsage.DynamicDraw);
            
            indexBufferHandle = GL.CreateBuffer();
            GL.NamedBufferData(indexBufferHandle, InitialIndexBufferCapacity * sizeof(int), IntPtr.Zero, VertexBufferObjectUsage.DynamicDraw);
            
            // Bind buffers to vertex array
            GL.EnableVertexArrayAttrib(vertexArrayHandle, 0);
            GL.VertexArrayVertexBuffer(vertexArrayHandle, 0, vertexBufferHandle, IntPtr.Zero, Unsafe.SizeOf<Vector2>());
            GL.VertexArrayAttribFormat(vertexArrayHandle, 0, 2, VertexAttribType.Float, false, 0);
            GL.VertexArrayAttribBinding(vertexArrayHandle, 0, 0);

            GL.VertexArrayElementBuffer(vertexArrayHandle, indexBufferHandle);
            
            // Create glyph storage buffer
            glyphStorageBufferHandle = GL.CreateBuffer();
            GL.NamedBufferData(glyphStorageBufferHandle, InitialGlyphBufferCapacity * Unsafe.SizeOf<RenderGlyph>(), IntPtr.Zero, VertexBufferObjectUsage.DynamicDraw);

            // Initialize multi draw buffer
            multiDrawIndirectBufferHandle = GL.CreateBuffer();
            multiDrawStorageBufferHandle = GL.CreateBuffer();

            // Initialize shader program
            programHandle = LoaderUtil.LoadShaderProgram(loader, "UberVertex", "UberFragment");

            // Get uniform locations
            viewportSizeUniformLocation = GL.GetUniformLocation(programHandle, "uViewportSize");

            // Create the global TMPX SSBOs. They'll be (re)populated lazily by UpdateFontData when
            // the font set changes.
            curveSsboHandle = GL.CreateBuffer();
            curveIndexSsboHandle = GL.CreateBuffer();
            bandEntrySsboHandle = GL.CreateBuffer();
            shapeEntrySsboHandle = GL.CreateBuffer();

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
            legacyBloom = new LegacyBloom(loader);
            universalBloom = new UniversalBloom(loader);
            glitch = new Glitch(loader);
            grain = new Grain(loader);
            uberPost = new UberPost(loader);
        }

        #endregion
        
        // Push all current meshes into the lifecycle command list so that they are created in the next frame
        foreach (var (id, mesh) in this.renderingFactory.Meshes)
            meshLifecycleCommands.Add(new MeshLifecycleCommand(id, LifecycleCommandType.Create, (mesh.Vertices, mesh.Indices)));
        
        // Push all current texts into the lifecycle command list so that they are created in the next frame
        foreach (var (id, text) in this.renderingFactory.Texts)
            textLifecycleCommands.Add(new TextLifecycleCommand(id, LifecycleCommandType.Create, text.Glyphs));
        
        // Subscribe to events
        this.renderingFactory.FontBuffersUpdated += OnFontBuffersUpdated;
        this.renderingFactory.Meshes.ItemInserted += OnMeshInserted;
        this.renderingFactory.Meshes.ItemRemoved += OnMeshRemoved;
        this.renderingFactory.Texts.ItemInserted += OnTextInserted;
        this.renderingFactory.Texts.ItemRemoved += OnTextRemoved;
    }

    public void Dispose()
    {
        logger.LogInformation("Disposing OpenGL renderer");
        
        // Unsubscribe from events
        renderingFactory.FontBuffersUpdated -= OnFontBuffersUpdated;
        renderingFactory.Meshes.ItemInserted -= OnMeshInserted;
        renderingFactory.Meshes.ItemRemoved -= OnMeshRemoved;
        renderingFactory.Texts.ItemInserted -= OnTextInserted;
        renderingFactory.Texts.ItemRemoved -= OnTextRemoved;
        
        // Context already lost, no need to continue disposing
        if (surface.IsContextLost)
            return;
        
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
        GL.DeleteBuffer(glyphStorageBufferHandle);
        
        GL.DeleteBuffer(multiDrawIndirectBufferHandle);
        GL.DeleteBuffer(multiDrawStorageBufferHandle);
        
        GL.DeleteProgram(programHandle);
        GL.DeleteBuffer(curveSsboHandle);
        GL.DeleteBuffer(curveIndexSsboHandle);
        GL.DeleteBuffer(bandEntrySsboHandle);
        GL.DeleteBuffer(shapeEntrySsboHandle);
        
        // Dispose post processors
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
        textLifecycleCommands.Add(new TextLifecycleCommand(e.Id, LifecycleCommandType.Create, e.Item.Glyphs));
    }

    private void OnTextRemoved(object? sender, ObservableSparseSetEventArgs<Text> e)
    {
        textLifecycleCommands.Add(new TextLifecycleCommand(e.Id, LifecycleCommandType.Destroy));
    }
    
    private void OnMeshInserted(object? sender, ObservableSparseSetEventArgs<Mesh> e)
    {
        meshLifecycleCommands.Add(new MeshLifecycleCommand(e.Id, LifecycleCommandType.Create, (e.Item.Vertices, e.Item.Indices)));
    }

    private void OnMeshRemoved(object? sender, ObservableSparseSetEventArgs<Mesh> e)
    {
        meshLifecycleCommands.Add(new MeshLifecycleCommand(e.Id, LifecycleCommandType.Destroy));
    }

    public void ProcessFrame(IDrawDataProvider drawDataProvider)
    {
        var renderSize = surface.RenderSize;
        
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
        // from front to back to avoid overdraw
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

        // Set viewport-size uniform for sub-pixel dilation in the text vertex shader
        GL.Uniform2f(viewportSizeUniformLocation, currentFboSize.X, currentFboSize.Y);

        // Bind indirect buffer
        GL.BindBuffer(BufferTarget.DrawIndirectBuffer, multiDrawIndirectBufferHandle);
        
        // Bind storage buffers
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, BindMultiDraw, multiDrawStorageBufferHandle);
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, BindGlyphs, glyphStorageBufferHandle);
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, BindCurves, curveSsboHandle);
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, BindCurveIndices, curveIndexSsboHandle);
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, BindBandEntries, bandEntrySsboHandle);
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, BindShapeEntries, shapeEntrySsboHandle);
        
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
        
        // Present to window
        surface.Present(finalTexture, renderSize, new ColorRgba());
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
                        FirstIndex = meshInfo.IndexBufferAllocation.Offset,
                        BaseVertex = meshInfo.VertexBufferAllocation.Offset,
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
                        Count = 6,
                        InstanceCount = textInfo.GlyphCount,
                        FirstIndex = 0,
                        BaseVertex = 0,
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

    private void UpdateOpenGlData(Vector2i size)
    {
        UpdateMeshData();
        UpdateFontData();
        UpdateTextData();
        UpdateFboData(size);
    }

    private void UpdateMeshData()
    {
        // Loop through the lifecycle command list
        foreach (var command in meshLifecycleCommands)
        {
            switch (command.Type)
            {
                case LifecycleCommandType.Create:
                {
                    Debug.Assert(command.Data.HasValue);
                    
                    var (vertices, indices) = command.Data.Value;

                    var vtxAlloc = vertexBufferAllocator.Allocate(vertices.Length, (oldCapacity, newCapacity) =>
                    {
                        // Recreate vertex buffer with new capacity
                        var newBufferHandle = GL.CreateBuffer();
                        GL.NamedBufferData(newBufferHandle, newCapacity * Unsafe.SizeOf<Vector2>(), IntPtr.Zero, VertexBufferObjectUsage.DynamicDraw);
                        GL.CopyNamedBufferSubData(vertexBufferHandle, newBufferHandle, IntPtr.Zero, IntPtr.Zero, oldCapacity * Unsafe.SizeOf<Vector2>());
                        GL.DeleteBuffer(vertexBufferHandle);
                        vertexBufferHandle = newBufferHandle;
                        
                        // Bind new vertex buffer to vertex array
                        GL.VertexArrayVertexBuffer(vertexArrayHandle, 0, vertexBufferHandle, IntPtr.Zero, Unsafe.SizeOf<Vector2>());
                        
                        logger.LogInformation("Mesh vertex buffer reallocated from {OldCapacity} to {NewCapacity}", oldCapacity, newCapacity);
                    });
                    
                    var idxAlloc = indexBufferAllocator.Allocate(indices.Length, (oldCapacity, newCapacity) =>
                    {
                        // Recreate index buffer with new capacity
                        var newBufferHandle = GL.CreateBuffer();
                        GL.NamedBufferData(newBufferHandle, newCapacity * sizeof(int), IntPtr.Zero, VertexBufferObjectUsage.DynamicDraw);
                        GL.CopyNamedBufferSubData(indexBufferHandle, newBufferHandle, IntPtr.Zero, IntPtr.Zero, oldCapacity * sizeof(int));
                        GL.DeleteBuffer(indexBufferHandle);
                        indexBufferHandle = newBufferHandle;
                        
                        // Bind new index buffer to vertex array
                        GL.VertexArrayElementBuffer(vertexArrayHandle, indexBufferHandle);
                        
                        logger.LogInformation("Mesh index buffer reallocated from {OldCapacity} to {NewCapacity}", oldCapacity, newCapacity);
                    });
                    
                    // Upload vertex data
                    GL.NamedBufferSubData(vertexBufferHandle, vtxAlloc.Offset * Unsafe.SizeOf<Vector2>(), vertices.Length * Unsafe.SizeOf<Vector2>(), vertices);
                    GL.NamedBufferSubData(indexBufferHandle, idxAlloc.Offset * sizeof(int), indices.Length * sizeof(int), indices);
                    
                    // Store mesh info
                    meshInfos.EnsureCount(command.Id + 1);
                    meshInfos[command.Id] = new MeshInfo
                    {
                        VertexBufferAllocation = vtxAlloc,
                        IndexBufferAllocation = idxAlloc,
                        VertexCount = vertices.Length,
                        IndexCount = indices.Length
                    };
                    break;
                }
                case LifecycleCommandType.Destroy:
                {
                    var meshInfo = meshInfos[command.Id];
                    vertexBufferAllocator.Free(meshInfo.VertexBufferAllocation);
                    indexBufferAllocator.Free(meshInfo.IndexBufferAllocation);
                    meshInfos[command.Id] = default;
                    break;
                }
            }
        }
        
        // Clear the lifecycle command list
        meshLifecycleCommands.Clear();
    }
    
    private void UpdateFontData()
    {
        if (!fontBuffersDirty)
            return;

        fontBuffersDirty = false;
        
        var curves = renderingFactory.FontCurves.AsSpan();
        var curveIndices = renderingFactory.FontCurveIndices.AsSpan();
        var bandEntries = renderingFactory.FontBandEntries.AsSpan();
        
        var shapeEntries = renderingFactory.FontShapeEntries;
        var gpuShapeEntries = shapeEntries.Select(x => new GpuShapeEntry
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
        
        GL.NamedBufferData(curveSsboHandle, curves.Length * Unsafe.SizeOf<QuadraticCurve>(), curves, VertexBufferObjectUsage.DynamicDraw);
        GL.NamedBufferData(curveIndexSsboHandle, curveIndices.Length * sizeof(int), curveIndices, VertexBufferObjectUsage.DynamicDraw);
        GL.NamedBufferData(bandEntrySsboHandle, bandEntries.Length * Unsafe.SizeOf<BandEntry>(), bandEntries, VertexBufferObjectUsage.DynamicDraw);
        GL.NamedBufferData(shapeEntrySsboHandle, gpuShapeEntries.Length * Unsafe.SizeOf<GpuShapeEntry>(), gpuShapeEntries, VertexBufferObjectUsage.DynamicDraw);
        
        logger.LogInformation(
            "Font buffers updated, registered {Curves} curves, {CurveIndices} curve indices, {Bands} band entries, {Shapes} shape entries",
            renderingFactory.FontCurves.Length,
            renderingFactory.FontCurveIndices.Length,
            renderingFactory.FontBandEntries.Length,
            renderingFactory.FontShapeEntries.Length);
    }

    private void UpdateTextData()
    {
        // if (!textsDirty)
        //     return;
        //
        // textsDirty = false;
        //
        // // Clear existing data
        // glyphBuffer.Clear();
        //
        // // Rebuild text buffer, glyphs carry globally-valid ShapeEntryIndex values already (FontService
        // // remapped them at shaping time), so we append verbatim with no patching.
        // if (renderingFactory.Texts.Count > 0)
        // {
        //     var maxId = renderingFactory.Texts.Select(x => x.Key).Max();
        //     textInfos.EnsureCount(maxId + 1);
        //
        //     var textInfosSpan = CollectionsMarshal.AsSpan(textInfos);
        //     foreach (var (id, text) in renderingFactory.Texts)
        //     {
        //         ref var textInfo = ref textInfosSpan[id];
        //         textInfo.GlyphOffset = glyphBuffer.Length;
        //         textInfo.GlyphCount = text.Glyphs.Length;
        //         
        //         var gpuRenderGlyphs = text.Glyphs.Select(x => new GpuRenderGlyph
        //         {
        //             Color = x.Color,
        //             Transform = x.Transform,
        //             ShapeEntryIndex = x.ShapeEntryIndex,
        //         }).ToArray();
        //         
        //         glyphBuffer.Append(gpuRenderGlyphs);
        //     }
        //
        //     GL.NamedBufferData(glyphStorageBufferHandle, glyphBuffer.LengthInBytes, glyphBuffer.Data, VertexBufferObjectUsage.DynamicDraw);
        // }
        //
        // logger.LogInformation("Text buffer updated, registered {GlyphCount} glyphs", glyphBuffer.Length);
        
        // Loop through the lifecycle command list
        foreach (var command in textLifecycleCommands)
        {
            switch (command.Type)
            {
                case LifecycleCommandType.Create:
                {
                    var renderGlyphsData = command.Data;
                    Debug.Assert(renderGlyphsData != null);
                    
                    var renderGlyphs = renderGlyphsData.Select(x => new GpuRenderGlyph
                    {
                        Color = x.Color,
                        Transform = x.Transform,
                        ShapeEntryIndex = x.ShapeEntryIndex,
                    }).ToArray();

                    var glyphAlloc = glyphBufferAllocator.Allocate(renderGlyphs.Length, (oldCapacity, newCapacity) =>
                    {
                        // Recreate glyph buffer with new capacity
                        var newBufferHandle = GL.CreateBuffer();
                        GL.NamedBufferData(newBufferHandle, newCapacity * Unsafe.SizeOf<GpuRenderGlyph>(), IntPtr.Zero, VertexBufferObjectUsage.DynamicDraw);
                        GL.CopyNamedBufferSubData(glyphStorageBufferHandle, newBufferHandle, IntPtr.Zero, IntPtr.Zero, oldCapacity * Unsafe.SizeOf<GpuRenderGlyph>());
                        GL.DeleteBuffer(glyphStorageBufferHandle);
                        glyphStorageBufferHandle = newBufferHandle;

                        logger.LogInformation("Glyph buffer reallocated from {OldCapacity} to {NewCapacity}", oldCapacity, newCapacity);
                    });

                    // Upload glyph data
                    GL.NamedBufferSubData(glyphStorageBufferHandle, glyphAlloc.Offset * Unsafe.SizeOf<GpuRenderGlyph>(), renderGlyphs.Length * Unsafe.SizeOf<GpuRenderGlyph>(), renderGlyphs);

                    // Store text info
                    textInfos.EnsureCount(command.Id + 1);
                    textInfos[command.Id] = new TextInfo
                    {
                        GlyphOffset = glyphAlloc.Offset,
                        GlyphCount = renderGlyphs.Length
                    };
                    break;
                }
                case LifecycleCommandType.Destroy:
                {
                    var textInfo = textInfos[command.Id];
                    glyphBufferAllocator.Free(new Allocation(textInfo.GlyphOffset, textInfo.GlyphCount));
                    textInfos[command.Id] = default;
                    break;
                }
            }
        }
        
        // Clear the lifecycle command list
        textLifecycleCommands.Clear();
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