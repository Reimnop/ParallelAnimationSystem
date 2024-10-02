using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Desktop;

public unsafe class DesktopWindow : IWindow, IDisposable
{
    public string Title
    {
        get => GLFW.GetWindowTitle(window);
        set => GLFW.SetWindowTitle(window, value);
    }

    public Vector2i FramebufferSize
    {
        get
        {
            GLFW.GetFramebufferSize(window, out var width, out var height);
            return new Vector2i(width, height);
        }
    }
    
    public bool ShouldClose => GLFW.WindowShouldClose(window);

    private readonly Window* window;

    public DesktopWindow(string title, Vector2i size, GLContextSettings glContextSettings)
    {
        GLFW.WindowHint(WindowHintClientApi.ClientApi, glContextSettings.ES ? ClientApi.OpenGlEsApi : ClientApi.OpenGlApi);
        if (!glContextSettings.ES)
            GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);
        GLFW.WindowHint(WindowHintInt.ContextVersionMajor, glContextSettings.Version.Major);
        GLFW.WindowHint(WindowHintInt.ContextVersionMinor, glContextSettings.Version.Minor);
        
        window = GLFW.CreateWindow(size.X, size.Y, title, null, null);
    }

    public void SetSwapInterval(int interval)
    {
        GLFW.MakeContextCurrent(window);
        GLFW.SwapInterval(interval);
    }

    public void RequestAnimationFrame(AnimationFrameCallback callback)
    {
        GLFW.PollEvents();
        
        GLFW.MakeContextCurrent(window);
        var time = GLFW.GetTime();
        if (callback(time))
            GLFW.SwapBuffers(window);
    }

    public void Close()
    {
        GLFW.SetWindowShouldClose(window, true);
    }
    
    public void Dispose()
    {
        GLFW.DestroyWindow(window);
    }
}