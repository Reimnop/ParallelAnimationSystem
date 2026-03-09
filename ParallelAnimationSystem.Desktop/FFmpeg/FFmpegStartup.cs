using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Desktop.FFmpeg;

public static class FFmpegStartup
{
    public static void ConsumeOptions(
        string beatmapPath,
        string audioPath,
        int width,
        int height,
        bool useEgl,
        ulong? seed,
        RenderingBackend backend,
        bool enablePostProcessing,
        bool enableTextRendering,
        string ffmpegPath,
        string outputPath,
        string ffmpegArgs)
    {
        var services = new ServiceCollection();
        
        services.AddSingleton(new DesktopWindowSettings
        {
            Size = new Vector2i(width, height),
            VSync = false,
            UseEgl = useEgl
        });

        services.AddSingleton(new FFmpegSettings
        {
            ExecPath = ffmpegPath,
            Args = ffmpegArgs
        });

        services
            .AddPlatform<FFmpegWindow, FFmpegGlfwService, RenderQueue>(
                backend,
                false,
                enablePostProcessing, enableTextRendering)
            .AddTransient<FFmpegFrameGenerator>();
        
        // Build service provider
        using var serviceProvider = services.BuildServiceProvider();
        
        // Set random seed
        var rss = serviceProvider.GetRequiredService<RandomSeedService>();
        rss.Seed = seed ?? NumberUtil.SplitMix64((ulong)DateTimeOffset.Now.ToUnixTimeSeconds());
        
        // Start frame generator
        var frameGenerator = serviceProvider.GetRequiredService<FFmpegFrameGenerator>();
        frameGenerator.GenerateFrames(beatmapPath, audioPath, 60, outputPath);
    }
}