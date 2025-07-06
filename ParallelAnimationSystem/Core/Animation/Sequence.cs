using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core.Animation;

public class Sequence<TIn, TOut>
{
    public IReadOnlyList<Keyframe<TIn>> Keyframes => keyframes;
    public float Length => keyframes.Count == 0 ? 0.0f : keyframes[^1].Time;
    
    private readonly List<Keyframe<TIn>> keyframes;
    private readonly Interpolator<TIn, TOut> interpolator;

    public Sequence(IEnumerable<Keyframe<TIn>> keyframes, Interpolator<TIn, TOut> interpolator)
    {
        this.keyframes = keyframes.ToList();
        this.interpolator = interpolator;
        this.keyframes.Sort((x, y) => x.Time.CompareTo(y.Time));
    }
    
    public TOut? Interpolate(float time, object? context = null)
    {
        if (keyframes.Count == 0)
            return default;

        if (keyframes.Count == 1)
            return ResultFromSingleKeyframe(keyframes[0], context);

        if (time < keyframes[0].Time)
            return ResultFromSingleKeyframe(keyframes[0], context);
        
        if (time >= keyframes[^1].Time)
            return ResultFromSingleKeyframe(keyframes[^1], context);

        var index = keyframes.BinarySearchKey(time, kf => kf.Time, Comparer<float>.Default);
        index = index < 0 ? ~index - 1 : index;
        var first = keyframes[index];
        var second = keyframes[index + 1];

        var t = InverseLerp(first.Time, second.Time, time);
        return interpolator(first.Value, second.Value, second.Ease(t), context);
    }
    
    private TOut ResultFromSingleKeyframe(Keyframe<TIn> keyframe, object? context)
        => interpolator(keyframe.Value, keyframe.Value, 0.0f, context);
    
    private static float InverseLerp(float a, float b, float value)
        => (value - a) / (b - a);
}