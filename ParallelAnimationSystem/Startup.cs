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
        var rendererTask = Task.Run(() =>
        {
            // Since the renderer must all be on one thread, we must initialize it here
            // Then run it on the same thread
            renderer.Initialize();
            renderer.Run();
        });
        var appTask = Task.Run(() => app.RunAsync());

        // Wait for both tasks to complete
        await Task.WhenAll(rendererTask, appTask);
    }
}