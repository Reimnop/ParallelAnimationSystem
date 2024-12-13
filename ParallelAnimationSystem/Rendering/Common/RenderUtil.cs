namespace ParallelAnimationSystem.Rendering.Common;

public static class RenderUtil
{
    public static float EncodeIntDepth(int depth)
        => depth / 8388608.0f; // 2^23
}