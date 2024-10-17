namespace ParallelAnimationSystem.FFmpeg;

public class FrameData(int width, int height)
{
    public int Width { get; } = width;
    public int Height { get; } = height;
    public byte[] Data { get; } = new byte[width * height * 4];
}