namespace ParallelAnimationSystem.Desktop;

public sealed class DesktopWindowSettings(bool transparent, bool resizable, bool borderless, bool floating)
{
    public bool Transparent { get; } = transparent;
    public bool Resizable { get; } = resizable;
    public bool Borderless { get; } = borderless;
    public bool Floating { get; } = floating;
}
