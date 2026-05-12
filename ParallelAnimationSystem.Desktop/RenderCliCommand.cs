using DotMake.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Desktop.FFmpeg;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Desktop;

[CliCommand(
    Name = "render", 
    Description = "Render the beatmap to a video file using FFmpeg")]
public class RenderCliCommand : RootCliCommand
{
    [CliOption(Name = "ffmpeg-path", Description = "Path to the FFmpeg executable")]
    public string FFmpegPath { get; set; } = "ffmpeg";

    [CliOption(Name = "output-path", Alias = "o", Description = "Path to the output video file")]
    public required string OutputPath { get; set; }
    
    [CliOption(Name = "framerate", Description = "Frame rate of the output video")]
    public int Framerate { get; set; } = 60;

    [CliOption(Name = "ffmpeg-args", Description = "Output arguments to pass to FFmpeg")]
    public string FFmpegArgs { get; set; } = "-c:v libx264 -pix_fmt yuv420p -preset slow -c:a aac -b:a 192k -ac 2 -channel_layout stereo";

    public void Run()
    {
        var services = new ServiceCollection();
        
        services.AddSingleton(new DesktopWindowSettings
        {
            Size = new Vector2i(Width, Height),
            VSync = false,
            UseEgl = UseEgl
        });

        services.AddSingleton(new FFmpegSettings
        {
            ExecPath = FFmpegPath,
            Args = FFmpegArgs
        });

        services
            .AddPlatform<FFmpegWindow, FFmpegGlfwService, RenderQueue>(
                Backend,
                false,
                EnablePostProcessing, EnableTextRendering)
            .AddTransient<FFmpegFrameGenerator>();
        
        // Build service provider
        using var serviceProvider = services.BuildServiceProvider();
        
        // Set random seed
        var rss = serviceProvider.GetRequiredService<RandomSeedService>();
        rss.Seed = Seed ?? NumberUtil.SplitMix64((ulong)DateTimeOffset.Now.ToUnixTimeSeconds());
        
        // Start frame generator
        var frameGenerator = serviceProvider.GetRequiredService<FFmpegFrameGenerator>();
        frameGenerator.GenerateFrames(BeatmapPath, AudioPath, Framerate, OutputPath);
    }
}

