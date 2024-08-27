using Microsoft.Extensions.Logging;
using OpenTK.Audio.OpenAL;
using ParallelAnimationSystem.Audio.Stream;

namespace ParallelAnimationSystem.Audio;

public class AudioSystem(ILogger<AudioSystem> logger) : IDisposable
{
    private ALContext context;
    
    public void Initialize()
    {
        // Override the OpenAL library path on Windows
        if (OperatingSystem.IsWindows())
            OpenALLibraryNameContainer.OverridePath = "soft_oal.dll";
        
        var device = ALC.OpenDevice(null);
        if (device == ALDevice.Null)
            throw new InvalidOperationException("Failed to open audio device");
        
        logger.LogInformation("Using audio device '{DeviceName}'", ALC.GetString(device, AlcGetString.DeviceSpecifier));
        
        context = ALC.CreateContext(device, (int[]?)null);
        if (context == ALContext.Null)
            throw new InvalidOperationException("Failed to create audio context");
        
        if (!ALC.MakeContextCurrent(context))
            throw new InvalidOperationException("Failed to make audio context current");
        
        logger.LogInformation("OpenAL: {Version}", AL.Get(ALGetString.Version));
        
        logger.LogInformation("Audio system initialized");
    }
    
    public AudioPlayer CreatePlayer(IAudioStream stream) => new(this, stream);
    
    public void Dispose()
    {
        if (context != ALContext.Null)
            ALC.DestroyContext(context);
    }
}