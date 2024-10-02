using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
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
        serviceCollection.AddSingleton(startup.AppSettings);
        serviceCollection.AddLogging(startup.ConfigureLogging);
        serviceCollection.AddSingleton(startup.CreateWindowManager);
        serviceCollection.AddSingleton(startup.CreateRenderer);
        serviceCollection.AddSingleton<AudioSystem>();
        serviceCollection.AddSingleton<App>();
        serviceCollection.AddSingleton<IResourceManager>(x =>
        {
            var appResourceManager = new EmbeddedResourceManager(typeof(App).Assembly);
            
            var resourceManager = startup.CreateResourceManager(x);
            if (resourceManager is null)
                return appResourceManager;
            
            return new MergedResourceManager([resourceManager, appResourceManager]);
        });
        serviceCollection.AddSingleton(startup.CreateMediaProvider);

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
        var appShutdown = false;
        var appThread = new Thread(() =>
        {
            app.Run();
            
            var sw = Stopwatch.StartNew();
            var deltaTime = 0.0;
            
            // ReSharper disable once AccessToModifiedClosure
            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            while (!appShutdown)
            {
                var dt = sw.Elapsed.TotalSeconds;
                sw.Restart();
                
                deltaTime += dt;
                if (!app.ProcessFrame(deltaTime))
                    Thread.Yield();
                else
                    deltaTime = 0.0;
            }
            
            sw.Stop();
        });
        appThread.Start();
        
        // Enter the main loop
        while (!renderer.Window.ShouldClose)
            if (!renderer.ProcessFrame())
                Thread.Yield();
        
        // When renderer exits, we'll shut down the services
        appShutdown = true;
        
        // Wait for the app thread to finish
        appThread.Join();
    }
}