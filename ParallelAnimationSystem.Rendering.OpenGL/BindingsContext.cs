using OpenTK;
using ParallelAnimationSystem.Windowing.OpenGL;

namespace ParallelAnimationSystem.Rendering.OpenGL;

public class BindingsContext(IOpenGLWindow window) : IBindingsContext
{
    public IntPtr GetProcAddress(string procName)
        => window.GetProcAddress(procName);
}