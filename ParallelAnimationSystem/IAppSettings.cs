namespace ParallelAnimationSystem;

public interface IAppSettings
{
    int SwapInterval { get; }
    int WorkerCount { get; }
    ulong Seed { get; }
    bool EnableTextRendering { get; }
}