using System.Runtime.InteropServices;
using FFMpegCore;
using FFMpegCore.Pipes;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Rendering;
using StbVorbisSharp;

namespace ParallelAnimationSystem.FFmpeg;

public class FFmpegFrameGenerator(
    FFmpegParameters parameters,
    MediaContext mediaContext,
    AppCore appCore,
    IRenderingFactory renderingFactory,
    IRenderer renderer,
    ILogger<FFmpegFrameGenerator> logger)
{
    public void Generate()
    {
        logger.LogInformation("Starting frame generation to {OutputPath}", parameters.OutputPath);
        
        var audioFrames = CreateAudioFrames(out var channels, out var sampleRate, out var duration);
        var videoFrames = CreateVideoFrames(duration, out var frameRate);
        
        var audioFramesSource = new RawAudioPipeSource(audioFrames)
        {
            Channels = (uint) channels,
            SampleRate = (uint) sampleRate,
            Format = "s16le",
        };
        
        var videoFramesSource = new RawVideoPipeSource(videoFrames)
        {
            FrameRate = frameRate,
        };

        FFMpegArguments
            .FromPipeInput(videoFramesSource)
            .AddPipeInput(audioFramesSource)
            .OutputToFile(parameters.OutputPath, true, options => options
                .WithVideoCodec(parameters.VideoCodec)
                .WithAudioCodec(parameters.AudioCodec))
            .ProcessSynchronously();
    }
    
    private IEnumerable<IVideoFrame> CreateVideoFrames(float duration, out int frameRate)
    {
        frameRate = parameters.FrameRate;
        return CreateVideoFrames(duration, frameRate);
    }
    
    private IEnumerable<IVideoFrame> CreateVideoFrames(float duration, int frameRate)
    {
        var drawList = renderingFactory.CreateDrawList();
        
        var frameCount = (int) (duration * frameRate);
        for (var i = 0; i < frameCount; i++)
        {
            var t = i / (float) frameRate;
            
            // Process frame
            appCore.ProcessFrame(t, drawList);
            renderer.ProcessFrame(drawList);
            
            // Clear draw list for next frame
            drawList.Clear();
            
            var window = (FFmpegWindow) renderer.Window;
            var currentFrame = window.CurrentFrame;
            if (currentFrame is null)
                continue;
            
            yield return new FFmpegFrameData(currentFrame);
            
            logger.LogInformation("Rendered {FrameCount}/{TotalFrameCount} frames - {Progress}% complete", 
                i + 1,
                frameCount,
                i / (float) frameCount * 100.0f);
        }
    }

    private IEnumerable<IAudioSample> CreateAudioFrames(out int channels, out int sampleRate, out float duration)
    {
        var vorbis = Vorbis.FromMemory(File.ReadAllBytes(mediaContext.AudioPath));
        channels = vorbis.Channels;
        sampleRate = (int) (vorbis.SampleRate * parameters.Speed);
        duration = vorbis.LengthInSeconds / parameters.Speed;
        return CreateAudioFrames(vorbis);
    }

    private IEnumerable<IAudioSample> CreateAudioFrames(Vorbis vorbis)
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
}