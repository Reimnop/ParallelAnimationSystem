using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering.OpenGL;
using ParallelAnimationSystem.Rendering.OpenGLES;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.FFmpeg;

public static class FFmpegStartup
{
    public static void ConsumeOptions(
        string beatmapPath,
        string audioPath,
        string outputPath,
        int sizeX,
        int sizeY,
        bool useEgl,
        int framerate,
        string videoCodec,
        string audioCodec,
        ulong? seed,
        float speed,
        RenderingBackend backend,
        bool enablePostProcessing,
        bool enableTextRendering)
    {
        var appSettings = new AppSettings
        {
            AspectRatio = null,
            EnablePostProcessing = enablePostProcessing,
            EnableTextRendering = enableTextRendering,
        };
        
        var services = new ServiceCollection();

        // Register contexts
        services.AddSingleton(new FFmpegWindowSettings
        {
            Size = new Vector2i(sizeX, sizeY),
            UseEgl = useEgl,
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
            builder.UseWindow<FFmpegWindow>();
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
        services.AddScoped<FFmpegFrameGenerator>();
        
        using var serviceProvider = services.BuildServiceProvider();
        
        // Set random seed
        var randomSeedService = serviceProvider.GetRequiredService<RandomSeedService>();
        randomSeedService.Seed = seed ?? NumberUtil.SplitMix64((ulong)DateTimeOffset.Now.ToUnixTimeSeconds());
        
        // Load beatmap
        ReadBeatmap(beatmapPath, out var beatmapData, out var beatmapFormat);
        
        var beatmapService = serviceProvider.GetRequiredService<BeatmapService>();
        beatmapService.LoadBeatmap(beatmapData, beatmapFormat);
        
        using var scope = serviceProvider.CreateScope();
        
        var frameGenerator = scope.ServiceProvider.GetRequiredService<FFmpegFrameGenerator>();
        frameGenerator.Generate();
    }
    
    private static void ReadBeatmap(string beatmapPath, out string beatmapData, out BeatmapFormat beatmapFormat)
    {
        beatmapData = File.ReadAllText(beatmapPath);
        
        var extension = Path.GetExtension(beatmapPath).ToLowerInvariant();
        beatmapFormat = extension switch
        {
            ".lsb" => BeatmapFormat.Lsb,
            ".vgd" => BeatmapFormat.Vgd,
            _ => throw new NotSupportedException($"Unsupported beatmap format '{extension}'")
        };
    }
}