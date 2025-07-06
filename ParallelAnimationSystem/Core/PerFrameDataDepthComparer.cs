using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core;

public class PerFrameDataDepthComparer : IComparer<PerFrameBeatmapObjectData>
{
    public int Compare(PerFrameBeatmapObjectData x, PerFrameBeatmapObjectData y)
    {
        if (x == y)
            return 0;
        
        var renderDepthComparison = x.BeatmapObject.Data.RenderDepth.CompareTo(y.BeatmapObject.Data.RenderDepth);
        if (renderDepthComparison != 0) 
            return renderDepthComparison;
        var parentDepthComparison = y.ParentDepth.CompareTo(x.ParentDepth);
        if (parentDepthComparison != 0) 
            return parentDepthComparison;
        
        // Hash both IDs
        var xIdHash = x.BeatmapObject.Id.GetHashCode();
        var yIdHash = y.BeatmapObject.Id.GetHashCode();
        
        // Compare the hashes
        return xIdHash.CompareTo(yIdHash);
    }
}