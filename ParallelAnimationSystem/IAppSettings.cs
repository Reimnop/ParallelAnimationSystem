namespace ParallelAnimationSystem;

public interface IAppSettings
{
    int SwapInterval { get; }
    int WorkerCount { get; }
    ulong Seed { get; }
    float? AspectRatio { get; }
    bool EnablePostProcessing { get; }
    bool EnableTextRendering { get; }
}