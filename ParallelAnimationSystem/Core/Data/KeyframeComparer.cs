namespace ParallelAnimationSystem.Core.Data;

public class KeyframeComparer<T> : IComparer<T> where T : IKeyframe
{
    public int Compare(T? x, T? y)
        => x!.Time.CompareTo(y!.Time);
}