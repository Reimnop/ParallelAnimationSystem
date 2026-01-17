using System.Numerics;
using OpenTK.Graphics.Egl;
using OpenTK.Graphics.OpenGLES2;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Windowing.OpenGL;

namespace ParallelAnimationSystem.Android;

public class AndroidWindow(IntPtr display, IntPtr context, IntPtr surface) : IOpenGLWindow, IDisposable
{
    // Does nothing on Android
    public string Title
    {
        get => title;
        set => title = value;
    }

    public Vector2i FramebufferSize
    {
        get
        {
            if (!Egl.QuerySurface(display, surface, Egl.WIDTH, out var width))
                throw new Exception("Failed to query surface width");
            
            if (!Egl.QuerySurface(display, surface, Egl.HEIGHT, out var height))
                throw new Exception("Failed to query surface height");
            
            return new Vector2i(width, height);
        }
    }
    
    public bool ShouldClose { get; private set; }

    private string title = string.Empty;

    public void MakeContextCurrent()
    {
        if (!Egl.MakeCurrent(display, surface, surface, context))
            throw new Exception("Failed to make EGL context current");
    }
    
    public void Present(int framebuffer, Vector4 clearColor, Vector2i size, Vector2i offset)
    {
        MakeContextCurrent();
        
        var dstSize = FramebufferSize;
        
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebuffer);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        
        // Clear the default framebuffer
        GL.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
        GL.Viewport(0, 0, dstSize.X, dstSize.Y);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        // Blit the framebuffer to the default framebuffer
        GL.BlitFramebuffer(
            offset.X, offset.Y, size.X, size.Y,
            offset.X, offset.Y, size.X, size.Y,
            ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
        
        Egl.SwapBuffers(display, surface);
    }

    public void PollEvents()
    {
        // Not needed on Android
    }

    public void Close()
    {
        ShouldClose = true;
    }
    
    public void Dispose()
    {
        Egl.MakeCurrent(display, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        Egl.DestroySurface(display, surface);
        Egl.DestroyContext(display, context);
        Egl.Terminate(display);
    }
}