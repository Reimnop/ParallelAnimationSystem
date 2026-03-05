namespace ParallelAnimationSystem.Rendering.Common;

public class Font(int width, int height, byte[] atlas)
{
    public int Width => width;
    public int Height => height;
    public byte[] Atlas => atlas;
}