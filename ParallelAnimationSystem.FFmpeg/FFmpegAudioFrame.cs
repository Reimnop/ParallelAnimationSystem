using FFMpegCore.Pipes;

namespace ParallelAnimationSystem.FFmpeg;

public class FFmpegAudioFrame(byte[] data) : IAudioSample
{
    public void Serialize(Stream stream)
    {
        stream.Write(data, 0, data.Length);
    }

    public Task SerializeAsync(Stream stream, CancellationToken token)
    {
        return stream.WriteAsync(data, 0, data.Length, token);
    }
}