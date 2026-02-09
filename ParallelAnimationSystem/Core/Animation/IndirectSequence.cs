namespace ParallelAnimationSystem.Core.Animation;

public class IndirectSequence<TIn, TOut, TContext>(
    IndirectSequence<TIn, TOut, TContext>.Resolver resolver,
    Interpolator<TOut> interpolator,
    Func<TContext, TOut> defaultValueProvider)
{
    public delegate TOut Resolver(TIn value, TContext context);

    private readonly List<Keyframe<TIn>> keyframes = [];
    
    public void LoadKeyframes(IEnumerable<Keyframe<TIn>> newKeyframes)
    {
        keyframes.Clear();
        keyframes.AddRange(newKeyframes);
        keyframes.Sort((a, b) => a.Time.CompareTo(b.Time));
    }
    
    public TOut ComputeValueAt(float time, TContext context)
    {
        if (keyframes.Count == 0)
            return defaultValueProvider(context);
        
        if (time < keyframes[0].Time)
            return resolver(keyframes[0].Value, context);
        
        if (time >= keyframes[^1].Time)
            return resolver(keyframes[^1].Value, context);
        
        SequenceCommon.GetKeyframePair(keyframes, time, out var firstKeyframe, out var secondKeyframe);
        var t = SequenceCommon.GetMixFactor(time, firstKeyframe.Time, secondKeyframe.Time);
        var easeFunction = EaseFunctions.GetOrLinear(secondKeyframe.Ease);
        var firstValue = resolver(firstKeyframe.Value, context);
        var secondValue = resolver(secondKeyframe.Value, context);
        return interpolator(firstValue, secondValue, easeFunction(t));
    }
}