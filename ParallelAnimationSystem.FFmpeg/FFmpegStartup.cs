using System.Runtime.InteropServices;
using FFMpegCore;
using FFMpegCore.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Windowing;
using StbVorbisSharp;

namespace ParallelAnimationSystem.FFmpeg;

public class FFmpegStartup(FFmpegAppSettings appSettings, string beatmapPath, RenderingBackend backend) : IStartup
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
        var appSettings = new FFmpegAppSettings(
            new Vector2i(sizeX, sizeY),
            0,
            -1,
            seed < 0
                ? (ulong) DateTimeOffset.Now.ToUnixTimeMilliseconds()
                : (ulong) seed,
            null,
            enablePostProcessing,
            enableTextRendering
        );
        
        var startup = new FFmpegStartup(appSettings, beatmapPath, backend);
        using var app = startup.InitializeApp();

        var logger = app.ServiceProvider.GetRequiredService<ILogger<FFmpegStartup>>();
        
        var beatmapRunner = app.BeatmapRunner;
        var renderer = app.Renderer;
        
        using var vorbis = Vorbis.FromMemory(File.ReadAllBytes(audioPath));
        
        var step = 1.0f / framerate * speed;
        var duration = vorbis.LengthInSeconds;
        
        var videoFramesSource = new RawVideoPipeSource(CreateVideoFrames(step, duration, beatmapRunner, renderer, logger))
        {
            FrameRate = framerate,
        };

        var audioFramesSource = new RawAudioPipeSource(CreateAudioFrames(vorbis))
        {
            Channels = (uint) vorbis.Channels,
            SampleRate = (uint) (vorbis.SampleRate * speed),
            Format = "s16le",
        };

        FFMpegArguments
            .FromPipeInput(videoFramesSource)
            .AddPipeInput(audioFramesSource)
            .OutputToFile(outputPath, true, options => options
                .WithVideoCodec(videoCodec)
                .WithAudioCodec(audioCodec))
            .ProcessSynchronously();
    }

    private static IEnumerable<IVideoFrame> CreateVideoFrames(
        float step, float duration,
        BeatmapRunner beatmapRunner, IRenderer renderer, ILogger logger)
    {
        var i = 0;
        for (var t = 0.0f; t <= duration; t += step)
        {
            beatmapRunner.ProcessFrame(t);
            renderer.ProcessFrame();
            
            var window = (FFmpegWindow) renderer.Window;
            var currentFrame = window.CurrentFrame;
            if (currentFrame is null)
                continue;
            
            yield return new FFmpegFrameData(currentFrame);
            
            logger.LogInformation("Rendered {FrameCount} frames ({Time}/{Duration} seconds)", ++i, t, duration);
        }
    }

    private static IEnumerable<IAudioSample> CreateAudioFrames(Vorbis vorbis)
    {
        while (true)
        {
            vorbis.SubmitBuffer();
        
            if (vorbis.Decoded == 0)
                yield break;

            var sBuffer = vorbis.SongBuffer;
            var bBuffer = MemoryMarshal.Cast<short, byte>(sBuffer);
            yield return new FFmpegAudioFrame(bBuffer.ToArray());
        }
    }

    public IAppSettings AppSettings { get; } = appSettings;

    public void ConfigureLogging(ILoggingBuilder loggingBuilder)
        => loggingBuilder.AddConsole();

    public IResourceManager? CreateResourceManager(IServiceProvider serviceProvider)
        => null;

    public IWindowManager CreateWindowManager(IServiceProvider serviceProvider)
        => new FFmpegWindowManager();

    public IRenderer CreateRenderer(IServiceProvider serviceProvider)
        => backend switch
        {
            RenderingBackend.OpenGL => new Rendering.OpenGL.Renderer(
                serviceProvider.GetRequiredService<IAppSettings>(),
                serviceProvider.GetRequiredService<IWindowManager>(),
                serviceProvider.GetRequiredService<IResourceManager>(),
                serviceProvider.GetRequiredService<ILogger<Rendering.OpenGL.Renderer>>()),
            RenderingBackend.OpenGLES => new Rendering.OpenGLES.Renderer(
                serviceProvider.GetRequiredService<IAppSettings>(),
                serviceProvider.GetRequiredService<IWindowManager>(),
                serviceProvider.GetRequiredService<IResourceManager>(),
                serviceProvider.GetRequiredService<ILogger<Rendering.OpenGLES.Renderer>>()),
            _ => throw new NotSupportedException($"Rendering backend '{backend}' is not supported")
        };

    public IMediaProvider CreateMediaProvider(IServiceProvider serviceProvider)
        => new FFmpegMediaProvider(beatmapPath);
}