using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering.OpenGL;
using ParallelAnimationSystem.Rendering.OpenGLES;

namespace ParallelAnimationSystem.Desktop;

public static class DesktopStartup
{
    public static void ConsumeOptions(
        string beatmapPath,
        string audioPath,
        bool vsync,
        int workerCount,
        long seed,
        float speed,
        RenderingBackend backend,
        bool lockAspectRatio,
        bool enablePostProcessing,
        bool enableTextRendering)
    {
        var appSettings = new AppSettings
        {
            InitialSize = new Vector2i(1366, 768),
            SwapInterval = vsync ? 1 : 0,
            WorkerCount = workerCount,
            Seed = seed < 0
                ? (ulong) DateTimeOffset.Now.ToUnixTimeMilliseconds()
                : (ulong) seed,
            AspectRatio = lockAspectRatio ? 16.0f / 9.0f : null,
            EnablePostProcessing = enablePostProcessing,
            EnableTextRendering = enableTextRendering,
        };

        var services = new ServiceCollection();

        services.AddSingleton(new MediaContext
        {
            BeatmapPath = beatmapPath,
            AudioPath = audioPath,
        });
        
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
        });
        
        services.AddPAS(builder =>
        {
            builder.UseAppSettings(appSettings);
            builder.UseWindowManager<DesktopWindowManager>();
            builder.UseMediaProvider<DesktopMediaProvider>();
            switch (backend)
            {
                case RenderingBackend.OpenGL:
                    builder.UseOpenGLRenderer();
                    break;
                case RenderingBackend.OpenGLES:
                    builder.UseOpenGLESRenderer();
                    break;
            }
        });

        services.AddTransient<DesktopApp>();
        
        // Build service provider
        using var serviceProvider = services.BuildServiceProvider();
        
        // Start the app
        var app = serviceProvider.GetRequiredService<DesktopApp>();
        app.StartApp();
    }
}