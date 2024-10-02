namespace ParallelAnimationSystem.Desktop;

public sealed class DesktopAppSettings(int workerCount, ulong seed, bool enableTextRendering) : IAppSettings
{
    public int WorkerCount { get; } = workerCount;
    public ulong Seed { get; } = seed;
    public bool EnableTextRendering { get; } = enableTextRendering;
}