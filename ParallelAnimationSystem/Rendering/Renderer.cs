using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering.PostProcessing;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Rendering;

public class Renderer(Options options, ILogger<Renderer> logger)
{
    private NativeWindow? window;
    
    public bool Initialized => window is not null;
    public bool Exiting => window?.IsExiting ?? true;
    
    public int QueuedDrawListCount => drawListQueue.Count;

    // Synchronization
    private readonly object meshDataLock = new();
    
    // Rendering data
    private readonly ConcurrentQueue<DrawList> drawListQueue = [];
    private readonly Buffer vertexBuffer = new();
    private readonly Buffer indexBuffer = new();
    private bool newMeshData = true;
    
    // Post processors
    private readonly Hue hue = new();
    private readonly Bloom bloom = new();
    
    // OpenGL data
    private int vertexArrayHandle;
    private int vertexBufferHandle;
    private int indexBufferHandle;
    private int programHandle;
    private int mvpUniformLocation, zUniformLocation, colorUniformLocation;

    private Vector2i currentFboSize;
    private int fboTextureHandle, fboDepthBufferHandle;
    private int fboHandle;
    private int postProcessTextureHandle1, postProcessTextureHandle2;
    private int postProcessFboHandle;

    private readonly List<DrawData> opaqueDrawData = [];
    private readonly List<DrawData> transparentDrawData = [];
    
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

    public void Initialize()
    {
        GLFWProvider.CheckForMainThread = false;
        
        var nws = new NativeWindowSettings
        {
            Title = "Parallel Animation System",
            ClientSize = new Vector2i(1600, 900),
            API = ContextAPI.OpenGL,
            Profile = ContextProfile.Core,
            APIVersion = new Version(4, 6),
            IsEventDriven = false,
        };
        window = new NativeWindow(nws);
        
        // Set VSync
        window.VSync = options.VSync ? VSyncMode.On : VSyncMode.Off; 
        
        logger.LogInformation("Window created");
    }
    
    public void Run()
    {
        if (window is null)
            throw new InvalidOperationException("Renderer has not been initialized");
        
        // Initialize OpenGL data
        InitializeOpenGlData();
        
        // Enable multisampling
        GL.Enable(EnableCap.Multisample);
        
        // Render loop
        logger.LogInformation("Entering render loop");
        while (!window.IsExiting)
        {
            UpdateFrame();
            RenderFrame();
        }
    }

    public void SubmitDrawList(DrawList drawList)
    {
        drawListQueue.Enqueue(drawList);
    }

    private void InitializeOpenGlData()
    {
        Debug.Assert(window is not null);
        
        // We will exclusively use DSA for this project
        GL.CreateVertexArrays(1, out vertexArrayHandle);
        GL.CreateBuffers(1, out vertexBufferHandle);
        GL.CreateBuffers(1, out indexBufferHandle);

        // Upload data to GPU
        unsafe
        {
            var vertexBufferData = vertexBuffer.Data;
            var indexBufferData = indexBuffer.Data;
            
            fixed (byte* ptr = vertexBufferData)
                GL.NamedBufferData(vertexBufferHandle, vertexBufferData.Length, (IntPtr) ptr, BufferUsageHint.StaticDraw);
            
            fixed (byte* ptr = indexBufferData)
                GL.NamedBufferData(indexBufferHandle, indexBufferData.Length, (IntPtr) ptr, BufferUsageHint.StaticDraw);
        }
        
        newMeshData = false;
        
        // Bind buffers to vertex array
        GL.EnableVertexArrayAttrib(vertexArrayHandle, 0);
        GL.VertexArrayVertexBuffer(vertexArrayHandle, 0, vertexBufferHandle, IntPtr.Zero, Vector2.SizeInBytes);
        GL.VertexArrayAttribFormat(vertexArrayHandle, 0, 2, VertexAttribType.Float, false, 0);
        GL.VertexArrayAttribBinding(vertexArrayHandle, 0, 0);
                
        GL.VertexArrayElementBuffer(vertexArrayHandle, indexBufferHandle);
        
        // Initialize shader program
        var vertexShaderSource = ResourceUtil.ReadAllText("Resources.Shaders.SimpleUnlitVertex.glsl");
        var fragmentShaderSource = ResourceUtil.ReadAllText("Resources.Shaders.SimpleUnlitFragment.glsl");
        
        // Create shaders
        var vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        
        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        
        // Compile them
        GL.CompileShader(vertexShader);
        GL.CompileShader(fragmentShader);
        
        // Create program
        programHandle = GL.CreateProgram();
        GL.AttachShader(programHandle, vertexShader);
        GL.AttachShader(programHandle, fragmentShader);
        GL.LinkProgram(programHandle);
        
        // Clean up
        GL.DetachShader(programHandle, vertexShader);
        GL.DetachShader(programHandle, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
        
        // Get uniform locations
        mvpUniformLocation = GL.GetUniformLocation(programHandle, "uMvp");
        zUniformLocation = GL.GetUniformLocation(programHandle, "uZ");
        colorUniformLocation = GL.GetUniformLocation(programHandle, "uColor");
        
        // Initialize fbo
        
        // Initialize scene fbo
        var fboSize = window.ClientSize;
        GL.CreateTextures(TextureTarget.Texture2DMultisample, 1, out fboTextureHandle);
        GL.TextureStorage2DMultisample(fboTextureHandle, 4, SizedInternalFormat.Rgba8, fboSize.X, fboSize.Y, true);
        
        GL.CreateRenderbuffers(1, out fboDepthBufferHandle);
        GL.NamedRenderbufferStorageMultisample(fboDepthBufferHandle, 4, RenderbufferStorage.DepthComponent32f, fboSize.X, fboSize.Y);
        
        GL.CreateFramebuffers(1, out fboHandle);
        GL.NamedFramebufferTexture(fboHandle, FramebufferAttachment.ColorAttachment0, fboTextureHandle, 0);
        GL.NamedFramebufferRenderbuffer(fboHandle, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, fboDepthBufferHandle);
        
        // Initialize post process fbo
        GL.CreateTextures(TextureTarget.Texture2D, 1, out postProcessTextureHandle1);
        GL.TextureStorage2D(postProcessTextureHandle1, 1, SizedInternalFormat.Rgba8, fboSize.X, fboSize.Y);
        
        GL.CreateTextures(TextureTarget.Texture2D, 1, out postProcessTextureHandle2);
        GL.TextureStorage2D(postProcessTextureHandle2, 1, SizedInternalFormat.Rgba8, fboSize.X, fboSize.Y);
        
        GL.CreateFramebuffers(1, out postProcessFboHandle);
        // We will bind the texture later
        
        currentFboSize = fboSize;
        
        // Initialize post processors
        hue.Initialize();
        bloom.Initialize();
        
        logger.LogInformation("OpenGL initialized");
    }

    private void UpdateFrame()
    {
        Debug.Assert(window is not null);
        
        window.NewInputFrame();
        NativeWindow.ProcessWindowEvents(false);
    }
    
    private void RenderFrame()
    {
        Debug.Assert(window is not null);
        
        // Get the current draw list
        DrawList? drawList;
        while (!drawListQueue.TryDequeue(out drawList))
            Thread.Yield();
        
        var windowSize = window.ClientSize;
        
        // If window size is invalid, don't draw
        // and limit FPS
        if (windowSize.X <= 0 || windowSize.Y <= 0)
        {
            Thread.Sleep(50);
            return;
        }

        // Update OpenGL data
        UpdateOpenGlData();
        
        // Split draw list into opaque and transparent
        opaqueDrawData.Clear();
        transparentDrawData.Clear();
        
        foreach (var drawData in drawList)
        {
            // Discard draw data that is fully transparent, or outside of clipping range
            if (drawData.Color.A == 0.0f || drawData.Z < 0.0f || drawData.Z > 1.0f)
                continue;
            
            // Add to appropriate list
            if (drawData.Color.A == 1.0f)
                opaqueDrawData.Add(drawData);
            else
                transparentDrawData.Add(drawData);
        }
        
        // Sort transparent draw data back to front and opaque front to back
        opaqueDrawData.Sort((a, b) => a.Z.CompareTo(b.Z));
        transparentDrawData.Sort((a, b) => b.Z.CompareTo(a.Z));
        
        // Get camera matrix (view and projection)
        var camera = GetCameraMatrix(drawList.CameraData);
        
        // Render
        GL.Viewport(0, 0, currentFboSize.X, currentFboSize.Y);
        
        // Clear buffers
        var clearColor = drawList.ClearColor;
        GL.ClearNamedFramebuffer(fboHandle, ClearBuffer.Color, 0, ref clearColor.R);
        var depthClearValue = 1.0f;
        GL.ClearNamedFramebuffer(fboHandle, ClearBuffer.Depth, 0, ref depthClearValue);
        
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboHandle);
        
        // Use our program
        GL.UseProgram(programHandle);
        
        // Bind our vertex array
        GL.BindVertexArray(vertexArrayHandle);
        
        // Opaque pass, disable blending, enable depth testing
        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.DepthTest);
        
        // Draw each mesh
        foreach (var drawData in opaqueDrawData)
        {
            var mesh = drawData.Mesh;
            var transform = drawData.Transform * camera;
            
            // Set transform
            GL.UniformMatrix3(mvpUniformLocation, false, ref transform);
            GL.Uniform1(zUniformLocation, drawData.Z);
            GL.Uniform4(colorUniformLocation, drawData.Color);
            
            // Draw
            // TODO: We can probably glMultiDrawElementsBaseVertex here
            GL.DrawElementsBaseVertex(PrimitiveType.Triangles, mesh.IndexSize, DrawElementsType.UnsignedInt, mesh.IndexOffset * sizeof(int), mesh.VertexOffset);
        }
        
        // Transparent pass, enable blending, disable depth write
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Enable(EnableCap.Blend);
        GL.DepthMask(false);
        
        // Draw each mesh
        foreach (var drawData in transparentDrawData)
        {
            var mesh = drawData.Mesh;
            var transform = drawData.Transform * camera;

            // Set transform
            GL.UniformMatrix3(mvpUniformLocation, false, ref transform);
            GL.Uniform1(zUniformLocation, drawData.Z);
            GL.Uniform4(colorUniformLocation, drawData.Color);

            // Draw
            // TODO: We can probably glMultiDrawElementsBaseVertex here
            GL.DrawElementsBaseVertex(PrimitiveType.Triangles, mesh.IndexSize, DrawElementsType.UnsignedInt, mesh.IndexOffset * sizeof(int), mesh.VertexOffset);
        }
        
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
            windowSize.X, windowSize.Y,
            ClearBufferMask.ColorBufferBit,
            BlitFramebufferFilter.Linear);

        window.Context.SwapBuffers();
    }

    private int HandlePostProcessing(PostProcessingData data, int texture1, int texture2)
    {
        if (hue.Process(currentFboSize, data.HueShiftAngle, texture1, texture2))
            Swap(ref texture1, ref texture2); // Swap if we processed
        
        if (bloom.Process(currentFboSize, data.BloomIntensity, data.BloomDiffusion, texture1, texture2))
            Swap(ref texture1, ref texture2);
        
        return texture1;
    }

    private Matrix3 GetCameraMatrix(CameraData camera)
    {
        Debug.Assert(window is not null);
        
        var aspectRatio = window.ClientSize.X / (float) window.ClientSize.Y;
        var view = Matrix3.Invert(
            MathUtil.CreateScale(Vector2.One * camera.Scale) *
            MathUtil.CreateRotation(camera.Rotation) *
            MathUtil.CreateTranslation(camera.Position));
        var projection = Matrix3.CreateScale(1.0f / aspectRatio, 1.0f, 1.0f);
        return view * projection;
    }

    private void UpdateOpenGlData()
    {
        UpdateMeshData();
        UpdateFboData();
    }

    private void UpdateMeshData()
    {
        lock (meshDataLock)
        {
            if (!newMeshData)
                return;
            
            // Update our buffers with the new data
            newMeshData = false;
            
            unsafe
            {
                var vertexBufferData = vertexBuffer.Data;
                var indexBufferData = indexBuffer.Data;
                
                fixed (byte* ptr = vertexBufferData)
                    GL.NamedBufferSubData(vertexBufferHandle, IntPtr.Zero, vertexBufferData.Length, (IntPtr) ptr);
                
                fixed (byte* ptr = indexBufferData)
                    GL.NamedBufferSubData(indexBufferHandle, IntPtr.Zero, indexBufferData.Length, (IntPtr) ptr);
            }
        }
        
        logger.LogInformation("OpenGL mesh buffers updated, now at {VertexSize} vertices and {IndexSize} indices", vertexBuffer.Data.Length / Vector2.SizeInBytes, indexBuffer.Data.Length / sizeof(int));
    }

    private void UpdateFboData()
    {
        Debug.Assert(window is not null);

        var fboSize = window.ClientSize;
        if (fboSize == currentFboSize)
            return;
        
        // Delete old textures
        GL.DeleteTexture(fboTextureHandle);
        GL.DeleteRenderbuffer(fboDepthBufferHandle);
        GL.DeleteTexture(postProcessTextureHandle1);
        GL.DeleteTexture(postProcessTextureHandle2);
        
        // Create new textures
        GL.CreateTextures(TextureTarget.Texture2DMultisample, 1, out fboTextureHandle);
        GL.TextureStorage2DMultisample(fboTextureHandle, 4, SizedInternalFormat.Rgba8, fboSize.X, fboSize.Y, true);
        
        GL.CreateRenderbuffers(1, out fboDepthBufferHandle);
        GL.NamedRenderbufferStorageMultisample(fboDepthBufferHandle, 4, RenderbufferStorage.DepthComponent32f, fboSize.X, fboSize.Y);
        
        GL.CreateTextures(TextureTarget.Texture2D, 1, out postProcessTextureHandle1);
        GL.TextureStorage2D(postProcessTextureHandle1, 1, SizedInternalFormat.Rgba8, fboSize.X, fboSize.Y);
        
        GL.CreateTextures(TextureTarget.Texture2D, 1, out postProcessTextureHandle2);
        GL.TextureStorage2D(postProcessTextureHandle2, 1, SizedInternalFormat.Rgba8, fboSize.X, fboSize.Y);
        
        // Bind to fbo
        GL.NamedFramebufferTexture(fboHandle, FramebufferAttachment.ColorAttachment0, fboTextureHandle, 0);
        GL.NamedFramebufferRenderbuffer(fboHandle, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, fboDepthBufferHandle);

        currentFboSize = fboSize;
        
        logger.LogInformation("OpenGL framebuffer size updated, now at {Width}x{Height}", currentFboSize.X, currentFboSize.Y);
    }
    
    private static void Swap<T>(ref T a, ref T b)
    {
        (a, b) = (b, a);
    }
}