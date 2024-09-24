using System.Runtime.InteropServices;
using OpenTK.Audio.OpenAL;
using ParallelAnimationSystem.Audio.Stream;

namespace ParallelAnimationSystem.Audio;

public class AudioPlayer : IDisposable
{
    public IAudioStream Stream { get; }

    public TimeSpan Position
    {
        get
        {
            var sourceState = AL.GetSource(sourceHandle, ALGetSourcei.SourceState);
            if (sourceState == (int)ALSourceState.Stopped)
                return TimeSpan.Zero;
            
            var secOffset = AL.GetSource(sourceHandle, ALSourcef.SecOffset);
            return TimeSpan.FromSeconds(secOffset);
        }
    }
    
    public float Pitch
    {
        get => AL.GetSource(sourceHandle, ALSourcef.Pitch);
        set => AL.Source(sourceHandle, ALSourcef.Pitch, value);
    }
    
    public bool Playing => AL.GetSource(sourceHandle, ALGetSourcei.SourceState) == (int) ALSourceState.Playing;
    
    private readonly AudioSystem audioSystem;
    private readonly int sourceHandle;
    private readonly int bufferHandle;
    
    private bool disposed;
    
    public AudioPlayer(AudioSystem audioSystem, IAudioStream stream)
    {
        this.audioSystem = audioSystem;
        Stream = stream;
        
        // TODO: We should probably stream the audio data instead of loading it all at once
        using var memoryStream = new MemoryStream();
        Span<float> buffer = stackalloc float[1024];
        while (stream.NextBuffer(buffer) > 0)
            memoryStream.Write(MemoryMarshal.AsBytes(buffer));
        
        var data = memoryStream.ToArray();
        var sampleSize = sizeof(float) * stream.Channels;
        var dataSize = data.Length / sampleSize * sampleSize;
        
        // Initialize AL buffer
        bufferHandle = AL.GenBuffer();
        unsafe
        {
            fixed (byte* dataPtr = data)
                AL.BufferData(
                    bufferHandle, 
                    stream.Channels == 2 
                        ? ALFormat.StereoFloat32Ext
                        : ALFormat.MonoFloat32Ext, 
                    dataPtr, 
                    dataSize,
                    stream.SampleRate);
        }
        
        // Initialize AL source
        sourceHandle = AL.GenSource();
        AL.Source(sourceHandle, ALSourcei.Buffer, bufferHandle);
        AL.Source(sourceHandle, ALSourceb.Looping, false);
    }

    public void Play(TimeSpan? fromPosition = null)
    {
        AL.SourcePlay(sourceHandle);
    }
    
    public void Pause()
    {
        AL.SourcePause(sourceHandle);
    }
    
    public void Stop()
    {
        AL.SourceStop(sourceHandle);
    }

    public void Seek(TimeSpan time)
    {
        var sampleOffset = (int)(time.TotalSeconds * Stream.SampleRate);
        AL.Source(sourceHandle, ALSourcei.SampleOffset, sampleOffset);
    }
    
    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        GC.SuppressFinalize(this);
        
        // Stop AL source
        AL.SourceStop(sourceHandle);
        
        // Delete AL source
        AL.DeleteSource(sourceHandle);
        
        // Delete AL buffer
        AL.DeleteBuffer(bufferHandle);
    }
}