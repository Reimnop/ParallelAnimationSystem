using Pamx.Keyframes;

namespace ParallelAnimationSystem.Core.Data;

public interface IKeyframe
{
    public float Time { get; }
    public Ease Ease { get; }
}