using NVorbis;

namespace ParallelAnimationSystem.Audio.Stream;

public class VorbisAudioStream(System.IO.Stream stream) : IAudioStream, IDisposable
{
    private readonly VorbisReader reader = new(stream);
    
    public int Channels => reader.Channels;
    public int SampleRate => reader.SampleRate;
    public TimeSpan Duration => reader.TotalTime;
    public TimeSpan Position => reader.TimePosition;

    public int NextBuffer(Span<float> buffer)
        => reader.ReadSamples(buffer);

    public void Seek(TimeSpan time)
        => reader.TimePosition = time;

    public void Dispose()
    {
        reader.Dispose();
    }
}