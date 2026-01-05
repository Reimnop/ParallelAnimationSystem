using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core;

public class PerFrameDepthComparer : IComparer<PerFrameBeatmapObjectData>
{
    public int Compare(PerFrameBeatmapObjectData x, PerFrameBeatmapObjectData y)
    {
        if (x == y)
            return 0;
        
        var renderDepthComparison = y.BeatmapObject.RenderDepth.CompareTo(x.BeatmapObject.RenderDepth);
        if (renderDepthComparison != 0) 
            return renderDepthComparison;
        var parentDepthComparison = y.ParentDepth.CompareTo(x.ParentDepth);
        if (parentDepthComparison != 0) 
            return parentDepthComparison;
        var startTimeComparison = x.BeatmapObject.StartTime.CompareTo(y.BeatmapObject.StartTime);
        if (startTimeComparison != 0)
            return startTimeComparison;
        
        // Hash both IDs
        var xIdHash = x.BeatmapObject.Id.String.GetHashCode();
        var yIdHash = y.BeatmapObject.Id.String.GetHashCode();
        
        // Compare the hashes
        return xIdHash.CompareTo(yIdHash);
    }
}