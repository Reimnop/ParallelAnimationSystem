using Pamx.Common.Enum;

namespace ParallelAnimationSystem.Core.Data;

public readonly struct EventKeyframe<T>(float time, Ease ease, T value) : IKeyframe
{
    public float Time => time;
    public Ease Ease => ease;
    public T Value => value;
}