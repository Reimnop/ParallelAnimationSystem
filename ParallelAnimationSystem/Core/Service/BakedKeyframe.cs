using Pamx.Keyframes;

namespace ParallelAnimationSystem.Core.Service;

public class BakedKeyframe<T>(float time, Ease ease, T value) 
{
    public float Time { get; } = time;
    public Ease Ease { get; } = ease;
    public T Value { get; } = value;
}