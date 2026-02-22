using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering.OpenGL;
using ParallelAnimationSystem.Rendering.OpenGLES;
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
        var appSettings = new AppSettings
        {
            Seed = seed ?? NumberUtil.SplitMix64((ulong) DateTimeOffset.Now.ToUnixTimeSeconds()),
            AspectRatio = lockAspectRatio ? 16.0f / 9.0f : null,
            EnablePostProcessing = enablePostProcessing,
            EnableTextRendering = enableTextRendering,
        };

        var services = new ServiceCollection();

        services.AddSingleton(new DesktopWindowSettings
        {
            Size = new Vector2i(width, height),
            VSync = vsync,
            UseEgl = useEgl,
        });

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
            builder.UseWindow<DesktopWindow>();
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
        
#if DEBUG
        // Add ImGui platform backend
        services.AddSingleton<IImGuiPlatformBackend, ImGuiPlatformBackend>();
#endif

        services.AddTransient<DesktopApp>();
        
        // Build service provider
        using var serviceProvider = services.BuildServiceProvider();
        
        // Start the app
        var app = serviceProvider.GetRequiredService<DesktopApp>();
        app.StartApp();
    }
}