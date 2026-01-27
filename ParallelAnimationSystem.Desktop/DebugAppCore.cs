using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem.Desktop;

public class DebugAppCore(IServiceProvider sp, AppSettings appSettings, ResourceLoader loader, IMediaProvider mediaProvider, IRenderingFactory renderingFactory, ILogger<AppCore> logger)
    : AppCore(sp, appSettings, loader, mediaProvider, renderingFactory, logger)
{
    public BloomPostProcessingData? OverrideLegacyBloom
    {
        get
        {
            using (legacyBloomPostProcessingDataLock.EnterScope())
            {
                return legacyBloomPostProcessingData;
            }
        }
        set
        {
            using (legacyBloomPostProcessingDataLock.EnterScope())
            {
                legacyBloomPostProcessingData = value;
            }
        }
    }

    public BloomPostProcessingData? OverrideUniversalBloom
    {
        get 
        {
            using (universalBloomPostProcessingDataLock.EnterScope())
            {
                return universalBloomPostProcessingData;
            }
        }
        set
        {
            using (universalBloomPostProcessingDataLock.EnterScope())
            {
                universalBloomPostProcessingData = value;
            }
        }
    }
    
    private readonly Lock legacyBloomPostProcessingDataLock = new();
    private BloomPostProcessingData? legacyBloomPostProcessingData;
    
    private readonly Lock universalBloomPostProcessingDataLock = new();
    private BloomPostProcessingData? universalBloomPostProcessingData;
    
    protected override BloomPostProcessingData CreateLegacyBloomData(float intensity, bool isLegacy)
    {
        using (legacyBloomPostProcessingDataLock.EnterScope())
        {
            if (OverrideLegacyBloom.HasValue)
                return OverrideLegacyBloom.Value;
        }
        
        return base.CreateLegacyBloomData(intensity, isLegacy);
    }
    
    protected override BloomPostProcessingData CreateUniversalBloomData(float intensity, float diffusion, bool isUniversal)
    {
        using (universalBloomPostProcessingDataLock.EnterScope())
        {
            if (OverrideUniversalBloom.HasValue)
                return OverrideUniversalBloom.Value;
        }
        
        return base.CreateUniversalBloomData(intensity, diffusion, isUniversal);
    }
}