namespace ParallelAnimationSystem.Android;

public sealed class AndroidAppSettings(int swapInterval, int workerCount, ulong seed, bool enableTextRendering) : IAppSettings
{
    public int SwapInterval { get; } = swapInterval;
    public int WorkerCount { get; } = workerCount;
    public ulong Seed { get; } = seed;
    public bool EnableTextRendering { get; } = enableTextRendering;
}