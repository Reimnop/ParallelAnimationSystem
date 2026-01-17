using Pamx.Common.Enum;

namespace ParallelAnimationSystem.Core.Animation;

public struct SequenceKeyframe<T> : IKeyframe
{
    public required float Time { get; init; }
    public required Ease Ease { get; init; }
    public required T Value { get; init; }
    
    public static T ResolveToValue(SequenceKeyframe<T> keyframe, object? _)
        => keyframe.Value;
}