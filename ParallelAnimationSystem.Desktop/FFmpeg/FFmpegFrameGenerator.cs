using System.CommandLine.Parsing;
using System.Diagnostics;
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
    FFmpegSettings settings,
    ILogger<FFmpegFrameGenerator> logger) : IDisposable
{
    private readonly StreamWriter ffmpegLogWriter = new("ffmpeg_output.log");
    
    public void Dispose()
    {
        ffmpegLogWriter.Flush();
        ffmpegLogWriter.Dispose();
    }

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
        var renderQueue = (RenderQueue)sp.GetRequiredService<IRenderQueue>();
        var window = (FFmpegWindow)sp.GetRequiredService<IWindow>();
        
        var windowSize = window.FramebufferSize;
        
        // Start FFmpeg process
        var processStartInfo = new ProcessStartInfo(settings.ExecPath) 
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
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
        
        // Log FFmpeg output and errors
        ffmpegProcess.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
                ffmpegLogWriter.WriteLine(args.Data);
        };
        
        ffmpegProcess.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
                ffmpegLogWriter.WriteLine(args.Data);
        };
        
        ffmpegProcess.BeginOutputReadLine();
        ffmpegProcess.BeginErrorReadLine();
        
        logger.LogInformation("Started FFmpeg process with PID {PID}", ffmpegProcess.Id);
        
        // Generate frames
        logger.LogInformation("Rendering video to {OutputPath}", outputPath);
        
        var appCore = sp.GetRequiredService<AppCore>();
        
        // Load audio
        using var audioPlayer = AudioPlayer.Load(audioPath);
        
        // Preserve space for progress bar
        var duration = (float)audioPlayer.Length;
        var frameCount = (int)(duration * framerate);
        for (var i = 0; i < frameCount; i++)
        {
            var time = i / (float)framerate;
            appCore.ProcessFrame(time);
            renderQueue.ProcessFrame(renderer);

            var frameData = window.FrameData;
            ffmpegProcess.StandardInput.BaseStream.Write(frameData);
            
            RenderProgress(i + 1, frameCount);
        }
        
        // Wait for FFmpeg process to finish
        ffmpegProcess.StandardInput.Close();
        ffmpegProcess.WaitForExit();
        
        logger.LogInformation("Finished rendering video");
    }
    
    private static void RenderProgress(int frame, int totalFrames)
    {
        const int barWidth = 20;

        var bar = $"[{new string('#', (int)(frame / (float)totalFrames * barWidth)),-barWidth}] " +
                  $"{frame}/{totalFrames} " +
                  $"({(frame / (float)totalFrames * 100):0.00}%)";

        // Clear + redraw
        Console.Write("\e[2K");
        Console.WriteLine(bar);
        
        // Move back up a line
        Console.Write("\e[1A");
    }
}