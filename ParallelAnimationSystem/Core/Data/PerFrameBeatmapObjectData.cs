using System.Numerics;
using ParallelAnimationSystem.Core.Beatmap;

namespace ParallelAnimationSystem.Core.Data;

public class PerFrameBeatmapObjectData
{
    public BeatmapObject? BeatmapObject { get; set; }
    public Matrix3x2 Transform { get; set; }
    public BeatmapObjectColor Color { get; set; }
    public int ParentDepth { get; set; }
}