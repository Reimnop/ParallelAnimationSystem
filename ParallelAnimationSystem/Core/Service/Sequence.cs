namespace ParallelAnimationSystem.Core.Service;

public class Sequence<T>(Interpolator<T> interpolator, Func<T> defaultValueProvider)
{
    private readonly List<Keyframe<T>> keyframes = [];

    public void LoadKeyframes(IEnumerable<Keyframe<T>> keyframes)
    {
        this.keyframes.Clear();
        this.keyframes.AddRange(keyframes);
        this.keyframes.Sort((a, b) => a.Time.CompareTo(b.Time));
    }

    public T ComputeValueAt(float time)
    {
        if (keyframes.Count == 0)
            return defaultValueProvider();
        
        if (time < keyframes[0].Time)
            return keyframes[0].Value;
        
        if (time >= keyframes[^1].Time)
            return keyframes[^1].Value;
        
        SequenceCommon.GetKeyframePair(keyframes, time, out var firstKeyframe, out var secondKeyframe);
        var t = SequenceCommon.GetMixFactor(time, firstKeyframe.Time, secondKeyframe.Time);
        var easeFunction = EaseFunctions.GetOrLinear(secondKeyframe.Ease);
        return interpolator(firstKeyframe.Value, secondKeyframe.Value, easeFunction(t));
    }
}