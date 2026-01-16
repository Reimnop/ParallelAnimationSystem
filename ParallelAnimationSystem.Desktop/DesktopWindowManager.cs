using OpenTK.Windowing.GraphicsLibraryFramework;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Windowing.OpenGL;

namespace ParallelAnimationSystem.Desktop;

public class DesktopWindowManager : IOpenGLWindowManager, IDisposable
{
    public DesktopWindowManager()
        => GLFW.Init();

    public void PollEvents()
        => GLFW.PollEvents();

    public IOpenGLWindow CreateWindow(string title, Vector2i size, OpenGLSettings glSettings)
        => new DesktopWindow(title, size, glSettings);

    public IntPtr GetProcAddress(string procName)
        => GLFW.GetProcAddress(procName);

    public void Dispose()
        => GLFW.Terminate();
}