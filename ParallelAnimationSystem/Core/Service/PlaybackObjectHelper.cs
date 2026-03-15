using Pamx.Objects;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Service;

public static class PlaybackObjectHelper
{
    public static float CalculateEndTime(float startTime, float duration, AutoKillType autoKillType, float autoKillOffset)
        => autoKillType switch
        {
            AutoKillType.NoAutoKill => float.PositiveInfinity,
            AutoKillType.LastKeyframe => startTime + duration,
            AutoKillType.LastKeyframeOffset => startTime + duration + autoKillOffset,
            AutoKillType.FixedTime => startTime + autoKillOffset,
            AutoKillType.SongTime => autoKillOffset,
            _ => float.PositiveInfinity // no auto kill by default
        };
    
    public static IEnumerable<BakedKeyframe<BeatmapObjectIndexedColor>> ResolveColorKeyframes(IEnumerable<Data.Keyframe<BeatmapObjectIndexedColor>> keyframes)
    {
        foreach (var kf in keyframes)
        {
            var value = new BeatmapObjectIndexedColor
            {
                ColorIndex1 = kf.Value.ColorIndex1,
                ColorIndex2 = kf.Value.ColorIndex2,
                Opacity = kf.Value.Opacity
            };
            
            yield return new BakedKeyframe<BeatmapObjectIndexedColor>(kf.Time, kf.Ease, value);
        }
    }
}