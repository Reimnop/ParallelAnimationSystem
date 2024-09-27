using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Audio;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Rendering.OpenGL;

namespace ParallelAnimationSystem;

public static class Startup
{
    public static void StartApp(Options options)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(options);
        serviceCollection.AddLogging(x => x.AddConsole());
        serviceCollection.AddSingleton<IRenderer, Renderer>();
        serviceCollection.AddSingleton<AudioSystem>();
        serviceCollection.AddSingleton<App>();

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