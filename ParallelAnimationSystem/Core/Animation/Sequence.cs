using ParallelAnimationSystem.Util;
using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Core.Animation;

public delegate T Interpolator<T>(T first, T second, float t);

public delegate T KeyframeResolver<in TKeyframe, in TContext, out T>(TKeyframe keyframe, TContext context) where TKeyframe : IKeyframe;

public class Sequence<TKeyframe, TContext, T> where TKeyframe : IKeyframe
{
    public IReadOnlyList<TKeyframe> Keyframes => keyframes;
    public float Length => keyframes.Count == 0 ? 0.0f : keyframes[^1].Time;
    
    private readonly List<TKeyframe> keyframes;
    private readonly KeyframeResolver<TKeyframe, TContext, T> resolver;
    private readonly Interpolator<T> interpolator;

    public Sequence(
        IEnumerable<TKeyframe> keyframes,
        KeyframeResolver<TKeyframe, TContext, T> resolver,
        Interpolator<T> interpolator)
    {
        this.keyframes = keyframes.ToList();
        this.resolver = resolver;
        this.interpolator = interpolator;
        this.keyframes.Sort((x, y) => x.Time.CompareTo(y.Time));
    }
    
    public T? Interpolate(float time, TContext context)
    {
        if (keyframes.Count == 0)
            return default;

        if (keyframes.Count == 1)
            return resolver(keyframes[0], context);

        if (time < keyframes[0].Time)
            return resolver(keyframes[0], context);
        
        if (time >= keyframes[^1].Time)
            return resolver(keyframes[^1], context);

        var index = keyframes.BinarySearchKey(time, kf => kf.Time, Comparer<float>.Default);
        index = index < 0 ? ~index - 1 : index;
        var first = keyframes[index];
        var second = keyframes[index + 1];
        
        var resolvedFirst = resolver(first, context);
        var resolvedSecond = resolver(second, context);
        var easeFunction = EaseFunctions.GetOrLinear(second.Ease);

        var t = MathUtil.InverseLerp(first.Time, second.Time, time);
        var easedT = easeFunction(t);
        
        return interpolator(resolvedFirst, resolvedSecond, easedT);
    }
}