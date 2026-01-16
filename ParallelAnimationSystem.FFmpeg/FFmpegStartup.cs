using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering.OpenGL;
using ParallelAnimationSystem.Rendering.OpenGLES;

namespace ParallelAnimationSystem.FFmpeg;

public static class FFmpegStartup
{
    public static void ConsumeOptions(
        string beatmapPath,
        string audioPath,
        string outputPath,
        int sizeX,
        int sizeY,
        int framerate,
        string videoCodec,
        string audioCodec,
        long seed,
        float speed,
        RenderingBackend backend,
        bool enablePostProcessing,
        bool enableTextRendering)
    {
        var appSettings = new AppSettings
        {
            WorkerCount = -1,
            Seed = seed < 0
                ? (ulong) DateTimeOffset.Now.ToUnixTimeMilliseconds()
                : (ulong) seed,
            AspectRatio = null,
            EnablePostProcessing = enablePostProcessing,
            EnableTextRendering = enableTextRendering,
        };
        
        var services = new ServiceCollection();

        // Register contexts
        services.AddSingleton(new FFmpegWindowSettings
        {
            Size = new Vector2i(sizeX, sizeY)
        });
        
        services.AddSingleton(new MediaContext
        {
            BeatmapPath = beatmapPath,
            AudioPath = audioPath
        });

        services.AddSingleton(new FFmpegParameters
        {
            FrameRate = framerate,
            Speed = speed,
            VideoCodec = videoCodec,
            AudioCodec = audioCodec,
            OutputPath = outputPath
        });
        
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
        });
        
        services.AddPAS(builder =>
        {
            builder.UseAppSettings(appSettings);
            builder.UseWindowManager<FFmpegWindowManager>();
            builder.UseMediaProvider<FFmpegMediaProvider>();
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
        
        // Register frame generator
        services.AddTransient<FFmpegFrameGenerator>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        var frameGenerator = serviceProvider.GetRequiredService<FFmpegFrameGenerator>();
        frameGenerator.Generate();
    }
}