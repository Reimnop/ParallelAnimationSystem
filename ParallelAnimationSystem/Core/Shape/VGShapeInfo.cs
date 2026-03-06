namespace ParallelAnimationSystem.Core.Shape;

public class VGShapeInfo(int sides, float roundness, float thickness, int sliceCount)
{
    public int Sides => sides;
    public float Roundness => roundness;
    public float Thickness => thickness;
    public int SliceCount => sliceCount;

    public override int GetHashCode()
        => HashCode.Combine(Sides, Roundness, Thickness, SliceCount);
}