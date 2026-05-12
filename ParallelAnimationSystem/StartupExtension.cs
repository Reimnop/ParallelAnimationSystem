using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Core.Text;

#if DEBUG
using ParallelAnimationSystem.DebugStuff;
#endif

namespace ParallelAnimationSystem;

public static class StartupExtension
{
    public static PASBuilder AddPAS(this IServiceCollection services)
    {
        // services.AddSingleton(options.AppSettings);

        // Everything related to resources are singletons
        // options.RenderingFactoryDefinition.RegisterToServiceCollection(services, ServiceLifetime.Singleton);
        // options.RenderQueueDefinition.RegisterToServiceCollection(services, ServiceLifetime.Singleton);

        // Add resource loader with all resource source factories
        // services.AddSingleton(_ => new ResourceLoader(options.ResourceSourceFactories
        //     .Append(() => new EmbeddedResourceSource(typeof(StartupExtension).Assembly))));

        var resourceSourceFactories = new ResourceSourceFactories();
        services.AddSingleton(resourceSourceFactories);
        
        // Add our own resource loader
        resourceSourceFactories.Add(() => new EmbeddedResourceSource(typeof(StartupExtension).Assembly));
        
        // Add resource loader
        services.AddSingleton<ResourceLoader>();

        // These manage rendering resources, so they should be singletons
        services.AddSingleton<MeshService>();
        services.AddSingleton<FontService>();

        // Add rendering services
        // options.WindowDefinition.RegisterToServiceCollection(services, ServiceLifetime.Scoped);
        // options.RendererDefinition.RegisterToServiceCollection(services, ServiceLifetime.Scoped);

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

#if DEBUG
        // Add ImGui
        services.AddScoped<ImGuiContext>();
        services.AddScoped<ImGuiBackend>();
#endif

        // Add migrations
        services.AddTransient<LsMigration>();
        services.AddTransient<VgMigration>();

        return new PASBuilder(services, resourceSourceFactories);
    }
}
