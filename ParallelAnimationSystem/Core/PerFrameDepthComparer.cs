using System.Diagnostics;
using System.Runtime.CompilerServices;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core;

public struct PerFrameDepthComparer : IComparer<PerFrameBeatmapObjectData>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(PerFrameBeatmapObjectData? x, PerFrameBeatmapObjectData? y)
    {
        Debug.Assert(x is not null);
        Debug.Assert(y is not null);
        Debug.Assert(x.BeatmapObject is not null);
        Debug.Assert(y.BeatmapObject is not null);
        
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