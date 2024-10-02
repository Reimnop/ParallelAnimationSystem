namespace ParallelAnimationSystem;

public interface IAppSettings
{
    int WorkerCount { get; }
    ulong Seed { get; }
    bool EnableTextRendering { get; }
}