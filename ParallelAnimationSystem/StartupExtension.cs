using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Core.Text;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem;

public static class StartupExtension
{
    public static PASBuilder AddPAS(this IServiceCollection services)
    {
        var resourceSourceFactories = new ResourceSourceFactories();
        services.AddSingleton(resourceSourceFactories);
        
        // Add our own resource loader
        resourceSourceFactories.Add(() => new EmbeddedResourceSource(typeof(StartupExtension).Assembly));

        services.AddSingleton<RenderQueue>();
        
        // Add resource loader
        services.AddSingleton<ResourceLoader>();

        // These manage rendering resources, so they should be singletons
        services.AddSingleton<MeshService>();
        services.AddSingleton<FontService>();

        // Add main services
        services.AddScoped<AppDirector>();
        services.AddScoped<AnimationPipeline>();
        services.AddScoped<PlaybackObjectSortingService>();
        services.AddScoped<Timeline>();
        services.AddScoped<ObjectSourceManager>();
        services.AddScoped<PlaybackObjectContainer>();
        services.AddScoped<ThemeManager>();
        services.AddScoped<PlaybackThemeContainer>();
        services.AddScoped<EventManager>();
        services.AddScoped<RandomSeedService>();
        services.AddScoped<BeatmapService>();
        services.AddScoped<TextShaper>();
        services.AddScoped<MeshCacheService>();
        services.AddScoped<TextCacheService>();

        // Add migrations
        services.AddTransient<LsMigration>();
        services.AddTransient<VgMigration>();

        return new PASBuilder(services, resourceSourceFactories);
    }
}
