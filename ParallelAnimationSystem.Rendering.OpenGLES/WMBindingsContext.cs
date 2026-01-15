using OpenTK;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Rendering.OpenGLES;

public class WMBindingsContext(IWindowManager windowManager) : IBindingsContext
{
    public IntPtr GetProcAddress(string procName)
        => windowManager.GetProcAddress(procName);
}