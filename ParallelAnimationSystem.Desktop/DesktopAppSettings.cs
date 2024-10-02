namespace ParallelAnimationSystem.Desktop;

public sealed class DesktopAppSettings(int swapInterval, int workerCount, ulong seed, float speed, bool enableTextRendering) : IAppSettings
{
    public int SwapInterval { get; } = swapInterval;
    public int WorkerCount { get; } = workerCount;
    public ulong Seed { get; } = seed;
    public float Speed { get; } = speed;
    public bool EnableTextRendering { get; } = enableTextRendering;
}