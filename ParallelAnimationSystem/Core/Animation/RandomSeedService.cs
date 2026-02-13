using Microsoft.Extensions.Logging;

namespace ParallelAnimationSystem.Core.Animation;

public class RandomSeedService
{
    public event EventHandler<ulong>? SeedChanged;
    
    public ulong Seed
    {
        get;
        set
        {
            if (field == value)
                return;
            
            field = value;
            SeedChanged?.Invoke(this, field);
        }
    }

    public RandomSeedService(AppSettings appSettings, ILogger<RandomSeedService> logger)
    {
        // initialize the seed from app settings
        Seed = appSettings.Seed;
        
        // log the initial seed
        logger.LogInformation("Using random seed '{Seed}'", Seed);
    }
}