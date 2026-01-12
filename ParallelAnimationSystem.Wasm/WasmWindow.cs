using OpenTK.Graphics.Egl;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Wasm;

public class WasmWindow : IWindow, IDisposable
{
    public string Title { get; set; }

    public Vector2i FramebufferSize
    {
        get
        {
            Egl.QuerySurface(display, surface, Egl.WIDTH, out var width);
            Egl.QuerySurface(display, surface, Egl.HEIGHT, out var height);
            return new Vector2i(width, height);
        }
    }

    public bool ShouldClose => false;

    private readonly IntPtr display;
    private readonly IntPtr context;
    private readonly IntPtr surface;
    
    public WasmWindow(IntPtr display, string title, GLContextSettings glContextSettings)
    {
        this.display = display;
        Title = title;
        
        var config = new[]
        {
            Egl.RED_SIZE, 8,
            Egl.GREEN_SIZE, 8,
            Egl.BLUE_SIZE, 8,
            Egl.ALPHA_SIZE, 0,
            Egl.DEPTH_SIZE, 0,
            Egl.STENCIL_SIZE, 0,
            Egl.SURFACE_TYPE, Egl.WINDOW_BIT,
            Egl.RENDERABLE_TYPE, glContextSettings.Version.Major == 3 ? Egl.OPENGL_ES3_BIT : Egl.OPENGL_ES2_BIT,
            Egl.SAMPLES, 0,
            Egl.NONE
        };

        var configs = new IntPtr[1];
        if (!Egl.ChooseConfig(display, config, configs, 1, out var numConfig))
            throw new InvalidOperationException("Failed to choose EGL config");
        
        if (numConfig == 0)
            throw new InvalidOperationException("No EGL configs found");
        
        var chosenConfig = configs[0];
        
        if (!Egl.BindAPI(RenderApi.ES))
            throw new InvalidOperationException("Failed to bind ES API");
        
        var ctxAttribs = new[]
        {
            Egl.CONTEXT_CLIENT_VERSION, glContextSettings.Version.Major,
            Egl.NONE
        };

        context = Egl.CreateContext(display, chosenConfig, IntPtr.Zero, ctxAttribs);
        if (context == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create EGL context");
        
        surface = Egl.CreateWindowSurface(display, chosenConfig, IntPtr.Zero, IntPtr.Zero);
        if (surface == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create EGL surface");
    }
    
    public void MakeContextCurrent()
    {
        if (!Egl.MakeCurrent(display, surface, surface, context))
            throw new InvalidOperationException("Failed to make context current");
    }

    public void SetSwapInterval(int interval)
    {
        // Do nothing
    }

    public void RequestAnimationFrame(AnimationFrameCallback callback)
    {
        MakeContextCurrent();
        callback(0, 0);
    }

    public void Close()
    {
        // Do nothing
    }

    public void Dispose()
    {
        Egl.DestroyContext(display, context);
        Egl.DestroySurface(display, surface);
    }
}