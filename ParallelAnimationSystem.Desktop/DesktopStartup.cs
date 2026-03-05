using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Util;

#if DEBUG
using ParallelAnimationSystem.DebugStuff;
using ParallelAnimationSystem.Desktop.DebugStuff;
#endif

namespace ParallelAnimationSystem.Desktop;

public static class DesktopStartup
{
    public static void ConsumeOptions(
        string beatmapPath,
        string audioPath,
        int width,
        int height,
        bool vsync,
        bool useEgl,
        ulong? seed,
        RenderingBackend backend,
        bool lockAspectRatio,
        bool enablePostProcessing,
        bool enableTextRendering)
    {
        var services = new ServiceCollection();
        
        services.AddSingleton(new DesktopWindowSettings
        {
            Size = new Vector2i(width, height),
            VSync = vsync,
            UseEgl = useEgl
        });

        services
            .AddPlatform<DesktopWindow, GlfwService>(
                backend,
                lockAspectRatio,
                enablePostProcessing, enableTextRendering)
            .AddTransient<DesktopApp>();
        
        // Build service provider
        using var serviceProvider = services.BuildServiceProvider();
        
        // Set random seed
        var rss = serviceProvider.GetRequiredService<RandomSeedService>();
        rss.Seed = seed ?? NumberUtil.SplitMix64((ulong)DateTimeOffset.Now.ToUnixTimeSeconds());
        
        // Start the app
        var app = serviceProvider.GetRequiredService<DesktopApp>();
        app.StartApp(beatmapPath, audioPath);
    }
}