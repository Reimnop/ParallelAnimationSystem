using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Windowing;

public interface IWindowManager
{
    IWindow CreateWindow(string title, Vector2i size, GLContextSettings glContextSettings);
    
    IntPtr GetProcAddress(string procName);
}