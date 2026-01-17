using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Desktop;

public class DesktopWindowSettings
{
    public required Vector2i Size { get; init; }
    public required bool VSync { get; init; }
    public required bool UseEgl { get; init; }
}