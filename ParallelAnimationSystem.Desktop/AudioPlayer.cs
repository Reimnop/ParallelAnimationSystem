using ManagedBass;

namespace ParallelAnimationSystem.Desktop;

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
    
    public static AudioPlayer Load(string path)
    {
        // Load the audio file
        var stream = Bass.CreateStream(path);
        if (stream == 0)
            throw new Exception($"Failed to load audio file '{Bass.LastError}'");
        return new AudioPlayer(stream);
    }

    public void Dispose()
    {
        Bass.StreamFree(stream);
    }
}