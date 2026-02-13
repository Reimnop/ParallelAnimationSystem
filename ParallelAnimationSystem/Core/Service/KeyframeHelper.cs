using System.Numerics;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Model;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core.Service;

public static class KeyframeHelper
{
    public static IEnumerable<Keyframe<Vector2>> ResolveRandomizableVector2Keyframes(
        IEnumerable<Vector2Keyframe> keyframes, ulong seed)
    {
        var prng = NumberUtil.CreatePseudoRng(seed);
        
        foreach (var kf in keyframes)
        {
            var resolvedValue = ParseRandomVector2(
                kf.RandomMode,
                kf.Value,
                kf.RandomValue,
                kf.RandomInterval,
                prng);
            yield return new Keyframe<Vector2>(kf.Time, kf.Ease, resolvedValue);
        }
    }
    
    public static IEnumerable<Keyframe<float>> ResolveRotationKeyframes(
        IEnumerable<RotationKeyframe> keyframes, ulong seed)
    {
        var prng = NumberUtil.CreatePseudoRng(seed);
        
        var currentRotation = 0f;
        foreach (var kf in keyframes)
        {
            var resolvedValue = ParseRandomFloat(
                kf.RandomMode,
                kf.Value,
                kf.RandomValue,
                kf.RandomInterval,
                prng);
            currentRotation = kf.IsRelative ? currentRotation + resolvedValue : resolvedValue;
            yield return new Keyframe<float>(kf.Time, kf.Ease, MathUtil.DegreesToRadians(currentRotation));
        }
    }
    
    private static float ParseRandomFloat(RandomMode randomMode, float value, float randomValue, float randomInterval, PseudoRng randomNext)
        => randomMode switch
        {
            RandomMode.None => value,
            RandomMode.Range => RoundToNearest(RandomRange(value, randomValue, randomNext), randomInterval),
            RandomMode.Snap => MathF.Round(RandomRange(value, value + randomInterval, randomNext)),
            RandomMode.Select => RandomFloat(randomNext) < 0.5f ? value : randomValue,
            _ => value
        };
    
    private static Vector2 ParseRandomVector2(RandomMode randomMode, Vector2 value, Vector2 randomValue, float randomInterval, PseudoRng randomNext)
        => randomMode switch
        {
            RandomMode.None => value,
            RandomMode.Range => new Vector2(
                RoundToNearest(RandomRange(value.X, randomValue.X, randomNext), randomInterval),
                RoundToNearest(RandomRange(value.Y, randomValue.Y, randomNext), randomInterval)),
            RandomMode.Snap => new Vector2(
                MathF.Round(RandomRange(value.X, value.X + randomInterval, randomNext)),
                MathF.Round(RandomRange(value.Y, value.Y + randomInterval, randomNext))),
            RandomMode.Select => RandomFloat(randomNext) < 0.5f ? value : randomValue,
            RandomMode.Scale => value * RandomRange(randomValue.X, randomValue.Y, randomNext),
            _ => value
        };
    
    private static float RoundToNearest(float value, float nearest)
    {
        if (nearest == 0.0f)
            return value;
        
        return MathF.Round(value / nearest) * nearest;
    }

    private static float RandomRange(float min, float max, PseudoRng randomNext)
        => float.Lerp(min, max, RandomFloat(randomNext));

    private static float RandomFloat(PseudoRng randomNext)
        => NumberUtil.UlongToFloat01(randomNext());
}