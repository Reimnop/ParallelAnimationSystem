using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Service;

public readonly struct Keyframe<T>(float time, Ease ease, T value) : IKeyframe 
{
    public float Time { get; } = time;
    public Ease Ease { get; } = ease;
    public T Value { get; } = value;
}