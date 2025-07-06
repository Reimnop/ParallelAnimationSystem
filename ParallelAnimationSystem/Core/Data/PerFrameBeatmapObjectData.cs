using System.Diagnostics.CodeAnalysis;
using OpenTK.Mathematics;
using ParallelAnimationSystem.Core.Beatmap;

namespace ParallelAnimationSystem.Core.Data;

public struct PerFrameBeatmapObjectData : IEquatable<PerFrameBeatmapObjectData>
{
    public BeatmapObject BeatmapObject { get; set; }
    public Matrix3 Transform { get; set; }
    public (Color4<Rgba>, Color4<Rgba>) Colors { get; set; }
    public int ParentDepth { get; set; }
    
    public static bool operator ==(PerFrameBeatmapObjectData left, PerFrameBeatmapObjectData right)
        => left.Equals(right);

    public static bool operator !=(PerFrameBeatmapObjectData left, PerFrameBeatmapObjectData right) 
        => !left.Equals(right);

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is PerFrameBeatmapObjectData other)
            return Equals(other);
        return false;
    }

    public bool Equals(PerFrameBeatmapObjectData other)
        => BeatmapObject == other.BeatmapObject && 
           Transform == other.Transform && 
           Colors == other.Colors && 
           ParentDepth == other.ParentDepth;

    public override int GetHashCode()
        => HashCode.Combine(BeatmapObject, Transform, Colors, ParentDepth);
}