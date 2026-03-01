namespace ParallelAnimationSystem.Desktop.FFmpeg;

public class FFmpegSettings
{
    public required string ExecPath { get; init; }
    public required bool EnablePreview { get; init; }
    public required string Args { get; init; }
}