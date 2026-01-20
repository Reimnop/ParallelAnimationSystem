using OpenTK;
using ParallelAnimationSystem.Windowing;
using ParallelAnimationSystem.Windowing.OpenGL;

namespace ParallelAnimationSystem.Rendering.OpenGLES;

public class BindingsContext(IOpenGLWindow window) : IBindingsContext
{
    public IntPtr GetProcAddress(string procName)
        => window.GetProcAddress(procName);
}