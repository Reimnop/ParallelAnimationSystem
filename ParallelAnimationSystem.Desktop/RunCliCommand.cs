using DotMake.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Platform.OpenGL;

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
        
        services.AddSingleton(new DesktopSurfaceSettings
        {
            Size = new Vector2i(Width, Height),
            VSync = VSync,
            UseEgl = UseEgl,
            LockAspectRatio = LockAspectRatio
        });

        services
            .AddPlatform<GlfwService>(Backend)
            .AddScoped<IOpenGLSurface, DesktopSurface>()
            .AddTransient<DesktopApp>();
        
        // Build service provider
        using var serviceProvider = services.BuildServiceProvider();
        
        // Start the app
        var app = serviceProvider.GetRequiredService<DesktopApp>();
        app.StartApp(BeatmapPath, AudioPath, Seed, EnablePostProcessing, EnableTextRendering);
    }
}