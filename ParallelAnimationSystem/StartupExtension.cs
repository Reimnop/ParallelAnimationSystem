using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem;

public static class StartupExtension
{
    public static App InitializeApp(this IStartup startup)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(startup.AppSettings);
        serviceCollection.AddLogging(startup.ConfigureLogging);
        serviceCollection.AddSingleton(startup.CreateWindowManager);
        serviceCollection.AddSingleton(startup.CreateRenderer);
        serviceCollection.AddSingleton<BeatmapRunner>();
        serviceCollection.AddSingleton<IResourceManager>(x =>
        {
            var appResourceManager = new EmbeddedResourceManager(typeof(StartupExtension).Assembly);
            
            var resourceManager = startup.CreateResourceManager(x);
            if (resourceManager is null)
                return appResourceManager;
            
            return new MergedResourceManager([resourceManager, appResourceManager]);
        });
        serviceCollection.AddSingleton(startup.CreateMediaProvider);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        // Get services
        var beatmapRunner = serviceProvider.GetRequiredService<BeatmapRunner>();
        var renderer = serviceProvider.GetRequiredService<IRenderer>();

        // Initialize them
        beatmapRunner.Initialize();
        renderer.Initialize();
        
        return new App(serviceProvider, renderer, beatmapRunner);
    }
}