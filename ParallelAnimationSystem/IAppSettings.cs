using OpenTK.Mathematics;

namespace ParallelAnimationSystem;

public interface IAppSettings
{
    Vector2i InitialSize { get; }
    int SwapInterval { get; }
    int WorkerCount { get; }
    ulong Seed { get; }
    float? AspectRatio { get; }
    float BackgroundOpacity { get; }
    bool EnablePostProcessing { get; }
    bool EnableTextRendering { get; }
}