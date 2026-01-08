using System.Diagnostics;
using OpenTK.Graphics.Egl;
using OpenTK.Mathematics;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Android;

public class AndroidWindow : IWindow, IDisposable
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

    private readonly IntPtr display;
    private readonly IntPtr context;
    private readonly IntPtr surface;

    private readonly Stopwatch stopwatch;

    private double currentTime = 0;

    public AndroidWindow(IntPtr display, IntPtr context, IntPtr surface)
    {
        this.display = display;
        this.context = context;
        this.surface = surface;
        
        stopwatch = Stopwatch.StartNew();
    }

    public void MakeContextCurrent()
    {
        if (!Egl.MakeCurrent(display, surface, surface, context))
            throw new Exception("Failed to make EGL context current");
    }

    public void SetSwapInterval(int interval)
    {
        if (!Egl.SwapInterval(display, interval))
            throw new Exception("Failed to set swap interval");
    }

    public void RequestAnimationFrame(AnimationFrameCallback callback)
    {
        MakeContextCurrent();
        
        var time = stopwatch.Elapsed.TotalSeconds;
        var deltaTime = time - currentTime;
        currentTime = time;

        if (callback(deltaTime, 0))
            Egl.SwapBuffers(display, surface);
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