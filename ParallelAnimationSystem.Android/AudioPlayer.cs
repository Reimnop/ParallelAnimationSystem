using ManagedBass;

namespace ParallelAnimationSystem.Android;

// Simple util class to play audio using BASS
public class AudioPlayer(int stream) : IDisposable
{
    static AudioPlayer()
    {
        Bass.Init();
    }
    
    public double Position
    {
        get => Bass.ChannelBytes2Seconds(stream, Bass.ChannelGetPosition(stream));
        set => Bass.ChannelSetPosition(stream, Bass.ChannelSeconds2Bytes(stream, value));
    }

    public double Frequency
    {
        get => Bass.ChannelGetAttribute(stream, ChannelAttribute.Frequency);
        set => Bass.ChannelSetAttribute(stream, ChannelAttribute.Frequency, value);
    }

    public double Length => Bass.ChannelBytes2Seconds(stream, Bass.ChannelGetLength(stream));
    
    public void Play()
    {
        Bass.ChannelPlay(stream);
    }
    
    public void Pause()
    {
        Bass.ChannelPause(stream);
    }
    
    public void Stop()
    {
        Bass.ChannelStop(stream);
    }
    
    public static AudioPlayer Load(ReadOnlySpan<byte> data)
    {
        var bassStream = Bass.CreateStream(data.ToArray(), 0, data.Length, BassFlags.Default);
        return new AudioPlayer(bassStream);
    }

    public void Dispose()
    {
        Bass.StreamFree(stream);
    }
}