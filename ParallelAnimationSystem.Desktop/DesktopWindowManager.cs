using OpenTK.Windowing.GraphicsLibraryFramework;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Windowing.OpenGL;

namespace ParallelAnimationSystem.Desktop;

public class DesktopWindowManager : IOpenGLWindowManager, IDisposable
{
    private readonly DesktopWindowSettings settings;

    public DesktopWindowManager(DesktopWindowSettings settings)
    {
        this.settings = settings;
        
        GLFW.Init();
    }

    public IOpenGLWindow CreateWindow(string title, OpenGLSettings glSettings)
        => new DesktopWindow(title, settings, glSettings);

    public IntPtr GetProcAddress(string procName)
        => GLFW.GetProcAddress(procName);

    public void Dispose()
        => GLFW.Terminate();
}