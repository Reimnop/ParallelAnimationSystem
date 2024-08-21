using System.Runtime.CompilerServices;

namespace ParallelAnimationSystem.Core.Animation;

public class Sequence<TIn, TOut>
{
    public ReadOnlySpan<Keyframe<TIn>> Keyframes => keyframes;
    
    private readonly Keyframe<TIn>[] keyframes;
    private readonly Interpolator<TIn, TOut> interpolator;

    public Sequence(IEnumerable<Keyframe<TIn>> keyframes, Interpolator<TIn, TOut> interpolator)
    {
        this.keyframes = keyframes.ToArray();
        this.interpolator = interpolator;
        Array.Sort(this.keyframes, (x, y) => x.Time.CompareTo(y.Time));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TOut? Interpolate(float time, object? context = null)
    {
        if (keyframes.Length == 0)
            return default;

        if (keyframes.Length == 1)
            return ResultFromSingleKeyframe(keyframes[0], context);

        if (time < keyframes[0].Time)
            return ResultFromSingleKeyframe(keyframes[0], context);
        
        if (time >= keyframes[^1].Time)
            return ResultFromSingleKeyframe(keyframes[^1], context);

        var index = Search(time);
        var first = keyframes[index];
        var second = keyframes[index + 1];

        var t = InverseLerp(first.Time, second.Time, time);
        return interpolator(first.Value, second.Value, second.Ease(t), context);
    }
    
    // Binary search for the keyframe pair that contains the given time
    private int Search(float time)
    {
        int low = 0;
        int high = keyframes.Length - 1;

        while (low <= high)
        {
            int mid = (low + high) / 2;
            float midTime = keyframes[mid].Time;

            if (time < midTime)
            {
                high = mid - 1;
            }
            else if (time > midTime)
            {
                low = mid + 1;
            }
            else
            {
                return mid;
            }
        }

        return low - 1;
    }
    
    private TOut ResultFromSingleKeyframe(Keyframe<TIn> keyframe, object? context)
        => interpolator(keyframe.Value, keyframe.Value, 0.0f, context);
    
    private static float InverseLerp(float a, float b, float value)
        => (value - a) / (b - a);
}