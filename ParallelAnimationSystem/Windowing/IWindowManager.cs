using OpenTK.Mathematics;

namespace ParallelAnimationSystem.Windowing;

public interface IWindowManager
{
    IOpenGLWindow CreateWindow(string title, Vector2i size, GLContextSettings glContextSettings);
    
    void PollEvents();
    
    IntPtr GetProcAddress(string procName);
}