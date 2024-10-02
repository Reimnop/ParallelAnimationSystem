namespace ParallelAnimationSystem.Desktop;

public sealed class DesktopAppSettings(int workerCount, ulong seed, float speed, bool enableTextRendering) : IAppSettings
{
    public int WorkerCount { get; } = workerCount;
    public ulong Seed { get; } = seed;
    public float Speed { get; } = speed;
    public bool EnableTextRendering { get; } = enableTextRendering;
}