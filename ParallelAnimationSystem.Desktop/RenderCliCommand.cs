using DotMake.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Desktop.FFmpeg;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering;

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

    [CliOption(Name = "start-time", Description = "Start time in seconds")]
    public float StartTime { get; set; } = 0f;

    [CliOption(Name = "duration", Description = "Duration to render in seconds")]
    public float? Duration { get; set; } = null;

    [CliOption(Name = "ffmpeg-args", Description = "Output arguments to pass to FFmpeg")]
    public string FFmpegArgs { get; set; } = "-c:v libx264 -pix_fmt yuv420p -preset slow -c:a aac -b:a 192k -ac 2 -channel_layout stereo";

    public void Run()
    {
        var services = new ServiceCollection();
        
        services.AddSingleton(new DesktopSurfaceSettings
        {
            Size = new Vector2i(Width, Height),
            VSync = false,
            UseEgl = UseEgl,
            LockAspectRatio = false,
        });

        services.AddSingleton(new FFmpegSettings
        {
            ExecPath = FFmpegPath,
            Args = FFmpegArgs
        });

        services
            .AddPlatform<FFmpegGlfwService>(Backend)
            .AddTransient<FFmpegFrameGenerator>();
        
        // Build service provider
        using var serviceProvider = services.BuildServiceProvider();
        
        // Start frame generator
        var frameGenerator = serviceProvider.GetRequiredService<FFmpegFrameGenerator>();
        frameGenerator.GenerateFrames(
            BeatmapPath, AudioPath, Framerate, OutputPath, Seed,
            EnablePostProcessing, EnableTextRendering,
            StartTime, Duration);
    }
}

