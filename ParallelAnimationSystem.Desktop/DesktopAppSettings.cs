using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Desktop;

public sealed class DesktopAppSettings(int swapInterval, int workerCount, ulong seed, float? aspectRatio, bool enablePostProcessing, bool enableTextRendering) : IAppSettings
{
    public Vector2i InitialSize { get; } = new(1366, 768);
    public int SwapInterval { get; } = swapInterval;
    public int WorkerCount { get; } = workerCount;
    public ulong Seed { get; } = seed;
    public float? AspectRatio { get; } = aspectRatio;
    public bool EnablePostProcessing { get; } = enablePostProcessing;
    public bool EnableTextRendering { get; } = enableTextRendering;
}