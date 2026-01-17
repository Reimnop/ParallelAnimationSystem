namespace ParallelAnimationSystem;

public class AppSettings
{
    public required int WorkerCount { get; init; }
    public required ulong Seed { get; init; }
    public required float? AspectRatio {  init;get; }
    public required bool EnablePostProcessing { get; init; }
    public required bool EnableTextRendering { get; init; }
}