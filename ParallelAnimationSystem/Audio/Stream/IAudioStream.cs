namespace ParallelAnimationSystem.Audio.Stream;

public interface IAudioStream
{
    int Channels { get; }
    int SampleRate { get; }
    TimeSpan Duration { get; }
    TimeSpan Position { get; }
    
    int NextBuffer(Span<float> buffer);
    void Seek(TimeSpan time);
}