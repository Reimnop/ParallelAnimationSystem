using ParallelAnimationSystem.Core;

namespace ParallelAnimationSystem.Android;

public class BeatmapContext
{
    public required string Data { get; init; }
    public required BeatmapFormat Format { get; init; }
}