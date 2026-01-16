using OpenTK;
using ParallelAnimationSystem.Windowing.OpenGL;

namespace ParallelAnimationSystem.Rendering.OpenGLES;

public class WMBindingsContext(IOpenGLWindowManager windowManager) : IBindingsContext
{
    public IntPtr GetProcAddress(string procName)
        => windowManager.GetProcAddress(procName);
}