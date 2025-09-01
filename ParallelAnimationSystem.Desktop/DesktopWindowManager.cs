using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Desktop;

public class DesktopWindowManager : IWindowManager, IDisposable
{
    private readonly DesktopWindowSettings windowSettings;

    public DesktopWindowManager(DesktopWindowSettings windowSettings)
    {
        this.windowSettings = windowSettings;
        
        GLFW.Init();
    }
    
    public IWindow CreateWindow(string title, Vector2i size, GLContextSettings glContextSettings)
        => new DesktopWindow(title, size, glContextSettings, windowSettings);

    public void PollEvents()
        => GLFW.PollEvents();

    public IntPtr GetProcAddress(string procName)
        => GLFW.GetProcAddress(procName);

    public void Dispose()
        => GLFW.Terminate();
}