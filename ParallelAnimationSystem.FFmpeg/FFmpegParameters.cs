namespace ParallelAnimationSystem.FFmpeg;

public class FFmpegParameters
{
    public required int FrameRate { get; init; }
    public required float Speed { get; init; }
    public required string VideoCodec { get; init; }
    public required string AudioCodec { get; init; }
    public required string OutputPath { get; init; }
}