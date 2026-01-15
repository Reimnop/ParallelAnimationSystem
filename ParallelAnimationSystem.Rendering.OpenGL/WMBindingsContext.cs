using OpenTK;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Rendering.OpenGL;

public class WMBindingsContext(IWindowManager windowManager) : IBindingsContext
{
    public IntPtr GetProcAddress(string procName)
        => windowManager.GetProcAddress(procName);
}