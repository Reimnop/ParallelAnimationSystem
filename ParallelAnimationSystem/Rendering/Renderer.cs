using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Rendering;

public class Renderer(ILogger<Renderer> logger)
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
    
    // OpenGL data
    private int vertexArrayHandle;
    private int vertexBufferHandle;
    private int indexBufferHandle;
    private int programHandle;

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
            NumberOfSamples = 4,
        };
        window = new NativeWindow(nws);
        
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
        var vertexShaderSource = ReadAllText("Resources.Shaders.SimpleUnlitVertex.glsl");
        var fragmentShaderSource = ReadAllText("Resources.Shaders.SimpleUnlitFragment.glsl");
        
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
        if (!drawListQueue.TryDequeue(out var drawList))
            return;
        
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
        GL.Viewport(0, 0, window.ClientSize.X, window.ClientSize.Y);
        GL.ClearColor(drawList.ClearColor);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        // Use our program
        GL.UseProgram(programHandle);
        
        // Bind our vertex array
        GL.BindVertexArray(vertexArrayHandle);
        
        // Opaque pass, disable blending, enable depth testing
        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthMask(true);
        
        // Draw each mesh
        foreach (var drawData in opaqueDrawData)
        {
            var mesh = drawData.Mesh;
            var transform = drawData.Transform * camera;
            
            // Set transform
            GL.UniformMatrix3(0, false, ref transform);
            GL.Uniform1(1, drawData.Z);
            GL.Uniform4(2, drawData.Color);
            
            // Draw
            // TODO: We can probably glMultiDrawElementsBaseVertex here
            GL.DrawElementsBaseVertex(PrimitiveType.Triangles, mesh.IndexSize, DrawElementsType.UnsignedInt, mesh.IndexOffset * sizeof(int), mesh.VertexOffset);
        }
        
        // Transparent pass, enable blending, disable depth write
        GL.Enable(EnableCap.Blend);
        GL.DepthMask(false);
        
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        
        // Draw each mesh
        foreach (var drawData in transparentDrawData)
        {
            var mesh = drawData.Mesh;
            var transform = drawData.Transform * camera;

            // Set transform
            GL.UniformMatrix3(0, false, ref transform);
            GL.Uniform1(1, drawData.Z);
            GL.Uniform4(2, drawData.Color);

            // Draw
            // TODO: We can probably glMultiDrawElementsBaseVertex here
            GL.DrawElementsBaseVertex(PrimitiveType.Triangles, mesh.IndexSize, DrawElementsType.UnsignedInt, mesh.IndexOffset * sizeof(int), mesh.VertexOffset);
        }
        
        // Restore depth write state
        GL.DepthMask(true);

        window.Context.SwapBuffers();
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
        
        logger.LogInformation("OpenGL buffers updated, now at {VertexSize} vertices and {IndexSize} indices", vertexBuffer.Data.Length / Vector2.SizeInBytes, indexBuffer.Data.Length / sizeof(int));
    }

    private static string ReadAllText(string path)
    {
        var assembly = typeof(Renderer).Assembly;
        path = $"{assembly.GetName().Name}.{path}";
        
        using var stream = assembly.GetManifestResourceStream(path);
        if (stream is null)
            throw new InvalidOperationException($"File '{path}' not found");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}