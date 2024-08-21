using ParallelAnimationSystem.Core.Animation;

namespace ParallelAnimationSystem.Util;

public static class SequenceUtil
{
    public static float GetSequenceLength<TIn, TOut>(this Sequence<TIn, TOut> sequence)
    {
        return sequence.Keyframes[^1].Time;
    }
}