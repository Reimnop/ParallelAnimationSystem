using System.Numerics;
using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Windowing.OpenGL;

public interface IOpenGLWindow : IWindow
{
    void MakeContextCurrent();
    void Present(int framebuffer, Vector4 clearColor, Vector2i size, Vector2i offset);
    
    IntPtr GetProcAddress(string procName);
}