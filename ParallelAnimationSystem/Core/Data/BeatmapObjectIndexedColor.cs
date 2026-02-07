namespace ParallelAnimationSystem.Core.Data;

public struct BeatmapObjectIndexedColor
{
    public required int ColorIndex1 { get; init; }
    public required int ColorIndex2 { get; init; }
    public required float Opacity { get; init; }
}