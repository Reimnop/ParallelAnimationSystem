using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Model;

namespace ParallelAnimationSystem.Core.Animation;

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
    
    public static IEnumerable<Keyframe<BeatmapObjectIndexedColor>> ResolveColorKeyframes(IEnumerable<BeatmapObjectColorKeyframe> keyframes)
    {
        foreach (var kf in keyframes)
        {
            var value = new BeatmapObjectIndexedColor
            {
                ColorIndex1 = kf.Color.ColorIndex1,
                ColorIndex2 = kf.Color.ColorIndex2,
                Opacity = kf.Color.Opacity
            };
            
            yield return new Keyframe<BeatmapObjectIndexedColor>(kf.Time, kf.Ease, value);
        }
    }
}