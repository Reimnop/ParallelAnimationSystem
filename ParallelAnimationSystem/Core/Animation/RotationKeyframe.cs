using Pamx.Common.Enum;

namespace ParallelAnimationSystem.Core.Animation;

public struct RotationKeyframe : IKeyframe
{
    public required float Time { get; init; }
    public required Ease Ease { get; init; }
    public required float Value { get; init; }
    
    public static float ResolveToValue(RotationKeyframe keyframe, object? _)
        => keyframe.Value;
}