using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Audio;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem;

public static class Startup
{
    public static void StartApp(IStartup startup)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(startup.ConfigureLogging);
        serviceCollection.AddSingleton(startup.CreateAppSettings());
        serviceCollection.AddSingleton(startup.CreateRenderer);
        serviceCollection.AddSingleton<AudioSystem>();
        serviceCollection.AddSingleton<App>();
        serviceCollection.AddSingleton<IResourceManager>(x =>
        {
            var resourceManagers = new List<IResourceManager>
            {
                startup.CreateResourceManager(x),
                new EmbeddedResourceManager(typeof(App).Assembly),
            };
            return new MergedResourceManager(resourceManagers);
        });

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        
        // Get services
        var renderer = serviceProvider.GetRequiredService<IRenderer>();
        var audioSystem = serviceProvider.GetRequiredService<AudioSystem>();
        var app = serviceProvider.GetRequiredService<App>();

        // Initialize them
        audioSystem.Initialize();
        app.Initialize();
        renderer.Initialize();
        
        // Run app thread
        var appThread = new Thread(app.Run);
        appThread.Start();
        
        // Enter the main loop
        while (!renderer.ShouldExit)
            renderer.ProcessFrame();
        
        // When renderer exits, we'll shut down the services
        app.Shutdown();
        
        // Wait for the app thread to finish
        appThread.Join();
    }
}