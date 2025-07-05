using OpenTK.Mathematics;
using ParallelAnimationSystem.Core.Beatmap;

namespace ParallelAnimationSystem.Core.Data;

public struct PerFrameBeatmapObjectData
{
    public BeatmapObject BeatmapObject { get; set; }
    public Matrix3 Transform { get; set; }
    public (Color4<Rgba>, Color4<Rgba>) Colors { get; set; }
}