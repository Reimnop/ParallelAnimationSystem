using OpenTK.Mathematics;

namespace ParallelAnimationSystem.FFmpeg;

public sealed class FFmpegAppSettings(Vector2i initialSize, int swapInterval, int workerCount, ulong seed, float? aspectRatio, bool enablePostProcessing, bool enableTextRendering) : IAppSettings
{
    public Vector2i InitialSize { get; } = initialSize;
    public int SwapInterval { get; } = swapInterval;
    public int WorkerCount { get; } = workerCount;
    public ulong Seed { get; } = seed;
    public float? AspectRatio { get; } = aspectRatio;
    public bool EnablePostProcessing { get; } = enablePostProcessing;
    public bool EnableTextRendering { get; } = enableTextRendering;
}