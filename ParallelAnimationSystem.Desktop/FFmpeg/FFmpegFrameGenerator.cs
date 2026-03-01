using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pamx.Common.Implementation;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Desktop.FFmpeg;

public class FFmpegFrameGenerator(
    IServiceProvider serviceProvider,
    IRenderingFactory renderingFactory,
    FFmpegSettings settings,
    ILogger<FFmpegFrameGenerator> logger)
{
    private readonly IDrawList drawList = renderingFactory.CreateDrawList();

    public void GenerateFrames(string beatmapPath, string audioPath, int framerate, string outputPath)
    {
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        
        // Load beatmap
        BeatmapHelper.ReadBeatmap(beatmapPath, out var beatmapData, out var beatmapFormat);
        var beatmapService = sp.GetRequiredService<BeatmapService>();
        beatmapService.LoadBeatmap(beatmapData, beatmapFormat);
        
        // Initialize renderer
        var renderer = sp.GetRequiredService<IRenderer>();
        var window = (FFmpegWindow)sp.GetRequiredService<IWindow>();
        
        var windowSize = window.FramebufferSize;
        
        // Start FFmpeg process
        var processStartInfo = new ProcessStartInfo(settings.ExecPath) 
        {
            UseShellExecute = false,
            RedirectStandardInput = true
        };
        
        processStartInfo.ArgumentList.AddRange([
            "-y",
            
            // input video args
            "-f", "rawvideo",
            "-pix_fmt", "rgba",
            "-s", $"{windowSize.X}x{windowSize.Y}",
            "-r", framerate.ToString(),
            "-i", "pipe:0",
            
            // input audio args
            "-c:a", "libvorbis",
            "-i", audioPath,
            
            // video filter
            "-vf", "vflip"
        ]);

        var outputArgs = CommandLineStringSplitter.Instance.Split(settings.Args);
        processStartInfo.ArgumentList.AddRange(outputArgs);
        processStartInfo.ArgumentList.Add(outputPath);
        
        using var ffmpegProcess = Process.Start(processStartInfo);
        if (ffmpegProcess == null)
            throw new InvalidOperationException("Failed to start FFmpeg process");
        
        logger.LogInformation("Started FFmpeg process with PID {PID}", ffmpegProcess.Id);
        
        // Generate frames
        logger.LogInformation("Rendering video frames");
        
        var appCore = sp.GetRequiredService<AppCore>();
        
        // Load audio
        using var audioPlayer = AudioPlayer.Load(audioPath);

        var duration = (float)audioPlayer.Length;
        var frameCount = (int)(duration * framerate);
        for (var i = 0; i < frameCount; i++)
        {
            logger.LogInformation("Rendering frame {Frame}/{TotalFrames}", i + 1, frameCount);
            
            if (settings.EnablePreview)
                window.PollEvents();
            
            var time = i / (float)framerate;
            appCore.ProcessFrame(time, drawList);
            renderer.ProcessFrame(drawList);

            var frameData = window.FrameData;
            ffmpegProcess.StandardInput.BaseStream.Write(frameData);
        }
        
        // Wait for FFmpeg process to finish
        ffmpegProcess.StandardInput.Close();
        ffmpegProcess.WaitForExit();
    }
}