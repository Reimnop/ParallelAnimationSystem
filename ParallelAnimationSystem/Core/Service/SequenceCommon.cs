using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core.Service;

public static class SequenceCommon
{
    public static void GetKeyframePair<T>(IReadOnlyList<BakedKeyframe<T>> keyframes, float time, out BakedKeyframe<T> left, out BakedKeyframe<T> right)
    {
        var index = keyframes.BinarySearchKey(time, kf => kf.Time, Comparer<float>.Default);
        index = index < 0 ? ~index - 1 : index;
        left = keyframes[index];
        right = keyframes[index + 1];
    }

    public static float GetMixFactor(float time, float left, float right)
        => (time - left) / (right - left);
}