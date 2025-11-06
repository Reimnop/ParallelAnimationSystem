namespace ParallelAnimationSystem.Core;

public class GameObjectDepthComparer : IComparer<GameObject>
{
    public int Compare(GameObject? x, GameObject? y)
    {
        if (ReferenceEquals(x, y)) 
            return 0;
        if (y is null) 
            return 1;
        if (x is null) 
            return -1;
        
        var renderDepthComparison = y.RenderDepth.CompareTo(x.RenderDepth);
        if (renderDepthComparison != 0) 
            return renderDepthComparison;
        var parentDepthComparison = y.ParentDepth.CompareTo(x.ParentDepth);
        if (parentDepthComparison != 0) 
            return parentDepthComparison;
        return x.StartTime.CompareTo(y.StartTime);
    }
}