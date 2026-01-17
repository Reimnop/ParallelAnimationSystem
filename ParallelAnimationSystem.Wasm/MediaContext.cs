using ParallelAnimationSystem.Core;

namespace ParallelAnimationSystem.Wasm;

public class MediaContext
{
    public required string BeatmapData { get; init; }
    public required BeatmapFormat BeatmapFormat { get; init; }
}