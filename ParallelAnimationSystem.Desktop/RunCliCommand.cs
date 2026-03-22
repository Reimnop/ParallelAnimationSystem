using DotMake.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Desktop;

[CliCommand(
    Name = "run",
    Description = "Run the beatmap in a window with real-time rendering")]
public class RunCliCommand : RootCliCommand
{
    [CliOption(Name = "vsync", Description = "Enable VSync")]
    public bool VSync { get; set; } = true;

    [CliOption(Name = "lock-aspect", Description = "Lock the aspect ratio to 16:9")]
    public bool LockAspectRatio { get; set; } = true;

    public void Run()
    {
        var services = new ServiceCollection();
        
        services.AddSingleton(new DesktopWindowSettings
        {
            Size = new Vector2i(Width, Height),
            VSync = VSync,
            UseEgl = UseEgl
        });

        services
            .AddPlatform<DesktopWindow, GlfwService, AsyncRenderQueue>(
                Backend,
                LockAspectRatio,
                EnablePostProcessing, EnableTextRendering)
            .AddTransient<DesktopApp>();
        
        // Build service provider
        using var serviceProvider = services.BuildServiceProvider();
        
        // Set random seed
        var rss = serviceProvider.GetRequiredService<RandomSeedService>();
        rss.Seed = Seed ?? NumberUtil.SplitMix64((ulong)DateTimeOffset.Now.ToUnixTimeSeconds());
        
        // Start the app
        var app = serviceProvider.GetRequiredService<DesktopApp>();
        app.StartApp(BeatmapPath, AudioPath, StartTime);
    }
}