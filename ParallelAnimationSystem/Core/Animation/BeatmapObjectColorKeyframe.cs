using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Animation;

public struct BeatmapObjectColorKeyframe : IKeyframe
{
    public required float Time { get; init; }
    public required Ease Ease { get; init; }
    public required int ColorIndex1 { get; init; }
    public required int ColorIndex2 { get; init; }
    public required float Opacity { get; init; }

    public static BeatmapObjectColor ResolveToValue(BeatmapObjectColorKeyframe keyframe, ThemeColorState context)
    {
        var colorIndex1 = Math.Clamp(keyframe.ColorIndex1, 0, context.Object.Count - 1);
        var colorIndex2 = Math.Clamp(keyframe.ColorIndex2, 0, context.Object.Count - 1);
        var opacity = keyframe.Opacity;
        
        var resolvedColor1 = context.Object[colorIndex1];
        var resolvedColor2 = context.Object[colorIndex2];
        return new BeatmapObjectColor(resolvedColor1, resolvedColor2, opacity);
    }
}