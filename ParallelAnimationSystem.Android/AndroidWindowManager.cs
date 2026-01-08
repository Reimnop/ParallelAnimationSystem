using System.Diagnostics;
using System.Runtime.InteropServices;
using Android.Runtime;
using Android.Views;
using OpenTK.Graphics.Egl;
using OpenTK.Mathematics;
using ParallelAnimationSystem.Windowing;
using IWindowManager = ParallelAnimationSystem.Windowing.IWindowManager;

namespace ParallelAnimationSystem.Android;

public class AndroidWindowManager : IWindowManager, IDisposable
{
    private const string LibAndroid = "libandroid.so";
    
    private AndroidWindow? window;
    private IntPtr aNativeWindowPtr;
    
    private readonly ISurfaceHolder surfaceHolder;

    public AndroidWindowManager(GraphicsSurfaceView surfaceView, ISurfaceHolder surfaceHolder)
    {
        this.surfaceHolder = surfaceHolder;
        
        surfaceView.SurfaceDestroyedCallback = _ => window?.Close();
    }
    
    public IWindow CreateWindow(string title, Vector2i size, GLContextSettings glContextSettings)
    {
        if (window is not null)
            throw new NotSupportedException("Only one window is supported on Android");
        
        if (!glContextSettings.ES)
            throw new NotSupportedException("Desktop OpenGL is not supported on Android, use OpenGL ES instead");
        
        // Initialize EGL
        var display = Egl.GetDisplay(Egl.DEFAULT_DISPLAY);
        
        Egl.Initialize(display, out _, out _);
        int[] configAttribs = 
        [
            Egl.RENDERABLE_TYPE, Egl.OPENGL_ES3_BIT,
            Egl.SURFACE_TYPE,    Egl.WINDOW_BIT,
            Egl.RED_SIZE,        8,
            Egl.GREEN_SIZE,      8,
            Egl.BLUE_SIZE,       8,
            Egl.ALPHA_SIZE,      0,
            Egl.NONE
        ];
        
        var configs = new IntPtr[1];
        Egl.ChooseConfig(display, configAttribs, configs, 1, out _);
        
        var config = configs[0];
        if (config == IntPtr.Zero)
            throw new Exception("Failed to choose EGL config");
        
        int[] contextAttribs = 
        [
            Egl.CONTEXT_CLIENT_VERSION, 3,
            Egl.NONE
        ];
        
        var context = Egl.CreateContext(display, config, IntPtr.Zero, contextAttribs);
        if (context == IntPtr.Zero)
            throw new Exception("Failed to create EGL context");
        
        var surface = surfaceHolder.Surface;
        Debug.Assert(surface is not null);
        
        aNativeWindowPtr = ANativeWindow_fromSurface(JNIEnv.Handle, surface.Handle);
        if (aNativeWindowPtr == IntPtr.Zero)
            throw new Exception("Failed to get ANativeWindow pointer from surface");
        
        var windowSurface = Egl.CreateWindowSurface(display, config, aNativeWindowPtr, IntPtr.Zero);
        if (windowSurface == IntPtr.Zero)
            throw new Exception("Failed to create EGL window surface");
        
        // Create window
        window = new AndroidWindow(display, context, windowSurface);
        return window;
    }

    public IntPtr GetProcAddress(string procName)
        => Egl.GetProcAddress(procName);

    public void Dispose()
    {
        window?.Dispose();
        
        if (aNativeWindowPtr != IntPtr.Zero)
        {
            ANativeWindow_release(aNativeWindowPtr);
            aNativeWindowPtr = IntPtr.Zero;
        }
    }
    
    [DllImport(LibAndroid)]
    private static extern IntPtr ANativeWindow_fromSurface(IntPtr env, IntPtr surface);
    
    [DllImport(LibAndroid)]
    public static extern void ANativeWindow_release(IntPtr window);
}