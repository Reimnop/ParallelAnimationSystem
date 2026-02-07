using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Model;

public struct BeatmapObjectColorKeyframe : IKeyframe
{
    public required float Time { get; init; }
    public required Ease Ease { get; init; }
    public required BeatmapObjectIndexedColor Color { get; init; }
}