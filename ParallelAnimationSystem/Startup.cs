using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem;

public static class Startup
{
    public static void StartApp(Options options)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(options);
        serviceCollection.AddLogging(x => x.AddConsole());
        serviceCollection.AddSingleton<App>();
        serviceCollection.AddSingleton<Renderer>();

        using var serviceProvider = serviceCollection.BuildServiceProvider();

        // Run app
        var app = serviceProvider.GetRequiredService<App>();

        // Initialize the app
        app.Initialize();

        // Run app thread
        var appThread = new Thread(app.Run);
        appThread.Start();
        
        // We'll run the renderer on the main thread
        var renderer = serviceProvider.GetRequiredService<Renderer>();
        renderer.Initialize();

        while (!renderer.ShouldExit)
            renderer.ProcessFrame();
        
        // When renderer exits, we'll shut down the app
        app.Shutdown();
        
        // When the renderer exits, we'll wait for the app thread to finish
        appThread.Join();
    }
}