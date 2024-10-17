using FFMpegCore.Pipes;

namespace ParallelAnimationSystem.FFmpeg;

public class FFmpegFrameData(FrameData frameData) : IVideoFrame
{
    public int Width => frameData.Width;
    public int Height => frameData.Height;
    public string Format => "rgba";
    
    public void Serialize(Stream pipe)
    {
        pipe.Write(frameData.Data, 0, frameData.Data.Length);
    }

    public Task SerializeAsync(Stream pipe, CancellationToken token)
    {
        return pipe.WriteAsync(frameData.Data, 0, frameData.Data.Length, token);
    }
}