using OpenTK;

namespace ParallelAnimationSystem.Windowing;

public class BindingsContext(IWindowManager windowManager) : IBindingsContext
{
    public IntPtr GetProcAddress(string procName)
        => windowManager.GetProcAddress(procName);
}