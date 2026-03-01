using OpenTK.Windowing.GraphicsLibraryFramework;
using ParallelAnimationSystem.Windowing.OpenGL;

namespace ParallelAnimationSystem.Desktop;

public class GlfwService : IDisposable
{
    // Called every time events are polled
    public event EventHandler? EventsPolled; 
    
    private readonly OpenGLSettings glSettings;
    
    public GlfwService(OpenGLSettings glSettings)
    {
        this.glSettings = glSettings;
        
        if (!GLFW.Init())
            throw new Exception("Failed to initialize GLFW");
    }
    
    public void Dispose()
    {
        GLFW.Terminate();
    }

    public unsafe Window* CreateWindowHandle(int width, int height, string title, bool useEgl)
    {
        if (glSettings.IsES)
        {
            GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlEsApi);
        }
        else
        {
            GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlApi);
            GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);
        }
        
        GLFW.WindowHint(WindowHintInt.ContextVersionMajor, glSettings.MajorVersion);
        GLFW.WindowHint(WindowHintInt.ContextVersionMinor, glSettings.MinorVersion);
        
        if (useEgl) 
            GLFW.WindowHint(WindowHintContextApi.ContextCreationApi, ContextApi.EglContextApi);

        SetWindowHints(width, height, title, useEgl);
        
        var window = GLFW.CreateWindow(width, height, title, null, null);
        if (window == null)
            throw new Exception("Failed to create GLFW window");
        
        return window;
    }
    
    public void PollEvents()
    {
        GLFW.PollEvents();
        EventsPolled?.Invoke(this, EventArgs.Empty);
    }
    
    protected virtual void SetWindowHints(int width, int height, string title, bool useEgl)
    {
    }
}