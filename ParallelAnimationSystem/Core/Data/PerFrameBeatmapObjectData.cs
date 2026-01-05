using OpenTK.Mathematics;
using ParallelAnimationSystem.Core.Beatmap;

namespace ParallelAnimationSystem.Core.Data;

public class PerFrameBeatmapObjectData
{
    public BeatmapObject? BeatmapObject { get; set; }
    public Matrix3 Transform { get; set; }
    public BeatmapObjectColor Color { get; set; }
    public int ParentDepth { get; set; }
}