using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core;

public class PerFrameDataDepthComparer : IComparer<PerFrameBeatmapObjectData>
{
    public int Compare(PerFrameBeatmapObjectData x, PerFrameBeatmapObjectData y)
    {
        if (x == y)
            return 0;
        
        var renderDepthComparison = y.BeatmapObject.Data.RenderDepth.CompareTo(x.BeatmapObject.Data.RenderDepth);
        if (renderDepthComparison != 0) 
            return renderDepthComparison;
        var parentDepthComparison = y.ParentDepth.CompareTo(x.ParentDepth);
        if (parentDepthComparison != 0) 
            return parentDepthComparison;
        var startTimeComparison = x.BeatmapObject.Data.StartTime.CompareTo(y.BeatmapObject.Data.StartTime);
        if (startTimeComparison != 0)
            return startTimeComparison;
        
        // Hash both IDs
        var xIdHash = x.BeatmapObject.Id.GetHashCode();
        var yIdHash = y.BeatmapObject.Id.GetHashCode();
        
        // Compare the hashes
        return xIdHash.CompareTo(yIdHash);
    }
}