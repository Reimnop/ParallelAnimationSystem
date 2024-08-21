namespace ParallelAnimationSystem.Core.Animation;

public record struct Keyframe<T>(float Time, T Value, EaseFunction Ease);