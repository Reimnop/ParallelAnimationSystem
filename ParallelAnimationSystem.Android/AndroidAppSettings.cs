namespace ParallelAnimationSystem.Android;

public sealed class AndroidAppSettings(int swapInterval, int workerCount, ulong seed, float? aspectRatio, bool enablePostProcessing, bool enableTextRendering) : IAppSettings
{
    public int SwapInterval { get; } = swapInterval;
    public int WorkerCount { get; } = workerCount;
    public ulong Seed { get; } = seed;
    public float? AspectRatio { get; } = aspectRatio;
    public bool EnablePostProcessing { get; } = enablePostProcessing;
    public bool EnableTextRendering { get; } = enableTextRendering;
}