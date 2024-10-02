namespace ParallelAnimationSystem;

public interface IAppSettings
{
    int WorkerCount { get; }
    ulong Seed { get; }
    float Speed { get; }
    bool EnableTextRendering { get; }
}