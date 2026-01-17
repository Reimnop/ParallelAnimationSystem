using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.FFmpeg;

public class FFmpegWindowSettings
{
    public required Vector2i Size { get; init; }
    public required bool UseEgl { get; init; }
}