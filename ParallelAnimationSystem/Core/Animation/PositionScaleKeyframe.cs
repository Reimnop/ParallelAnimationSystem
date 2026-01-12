using System.Numerics;
using Pamx.Common.Enum;

namespace ParallelAnimationSystem.Core.Animation;

public struct PositionScaleKeyframe : IKeyframe
{
    public required float Time { get; init; }
    public required Ease Ease { get; init; }
    public required Vector2 Value { get; init; }
    
    public static Vector2 ResolveToValue(PositionScaleKeyframe keyframe, object? _)
        => keyframe.Value;
}