using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem;

public static class Startup
{
    public static async Task StartAppAsync(Options options)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(options);
        serviceCollection.AddLogging(x => x.AddConsole());
        serviceCollection.AddSingleton<App>();
        serviceCollection.AddSingleton<Renderer>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Run app
        var app = serviceProvider.GetRequiredService<App>();
        var renderer = serviceProvider.GetRequiredService<Renderer>();

        // Initialize the app first
        app.Initialize();

        // Run the tasks
        var rendererThread = new Thread(() =>
        {
            // Since the renderer must all be on one thread, we must initialize it here
            // Then run it on the same thread
            renderer.Initialize();
            renderer.Run();
        });
        var appThread = new Thread(() => app.Run());
        
        rendererThread.Start();
        appThread.Start();

        // Asynchronously wait for both threads to exit
        await Task.WhenAll(
            Task.Run(() => rendererThread.Join()),
            Task.Run(() => appThread.Join())
        );
    }
}