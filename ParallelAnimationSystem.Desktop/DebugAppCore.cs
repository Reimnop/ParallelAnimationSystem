using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem.Desktop;

public class DebugAppCore(IServiceProvider sp, AppSettings appSettings, ResourceLoader loader, IMediaProvider mediaProvider, IRenderingFactory renderingFactory, ILogger<AppCore> logger)
    : AppCore(sp, appSettings, loader, mediaProvider, renderingFactory, logger)
{
    public BloomPostProcessingData? OverrideBloom
    {
        get
        {
            using (bloomPostProcessingDataLock.EnterScope())
            {
                return bloomPostProcessingData;
            }
        }
        set
        {
            using (bloomPostProcessingDataLock.EnterScope())
            {
                bloomPostProcessingData = value;
            }
        }
    }
    
    private readonly Lock bloomPostProcessingDataLock = new();
    private BloomPostProcessingData? bloomPostProcessingData;
    
    protected override BloomPostProcessingData CreateBloomData(float intensity, float diffusion)
    {
        if (OverrideBloom.HasValue)
            return OverrideBloom.Value;
        return base.CreateBloomData(intensity, diffusion);
    }
}