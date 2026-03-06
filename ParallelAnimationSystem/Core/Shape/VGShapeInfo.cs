namespace ParallelAnimationSystem.Core.Shape;

public class VGShapeInfo(float radius, int vertexCount, float cornerRoundness, float thickness, int sliceCount)
{
    public float Radius => radius;
    public int VertexCount => vertexCount;
    public float CornerRoundness => cornerRoundness;
    public float Thickness => thickness;
    public int SliceCount => sliceCount;

    public override int GetHashCode()
        => HashCode.Combine(Radius, VertexCount, CornerRoundness, Thickness, SliceCount);
}