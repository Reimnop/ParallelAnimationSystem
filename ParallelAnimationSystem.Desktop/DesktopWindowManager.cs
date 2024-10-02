using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Desktop;

public class DesktopWindowManager : IWindowManager, IDisposable
{
    public DesktopWindowManager()
        => GLFW.Init();
    
    public IOpenGLWindow CreateWindow(string title, Vector2i size, GLContextSettings glContextSettings)
        => new DesktopWindow(title, size, glContextSettings);

    public void PollEvents()
        => GLFW.PollEvents();

    public IntPtr GetProcAddress(string procName)
        => GLFW.GetProcAddress(procName);

    public void Dispose()
        => GLFW.Terminate();
}