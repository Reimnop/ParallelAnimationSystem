using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Desktop;

public class DesktopStartup(DesktopAppSettings appSettings, string beatmapPath, string audioPath, RenderingBackend backend) : IStartup
{
    public static void ConsumeOptions(
        string beatmapPath,
        string audioPath,
        bool vsync,
        int workerCount,
        long seed,
        float speed,
        RenderingBackend backend,
        bool lockAspectRatio,
        bool enablePostProcessing,
        bool enableTextRendering)
    {
        var appSettings = new DesktopAppSettings(
            vsync ? 1 : 0,
            workerCount,
            seed < 0
                ? (ulong) DateTimeOffset.Now.ToUnixTimeMilliseconds()
                : (ulong) seed,
            lockAspectRatio ? 16.0f / 9.0f : null,
            enablePostProcessing,
            enableTextRendering
        );
        
        var startup = new DesktopStartup(appSettings, beatmapPath, audioPath, backend);
        using var app = startup.InitializeApp();
        using var audioPlayer = AudioPlayer.Load(audioPath);
        var baseFrequency = audioPlayer.Frequency;
        audioPlayer.Frequency = baseFrequency * speed;
        
        var beatmapRunner = app.BeatmapRunner;
        var renderer = app.Renderer;
        
        var appShutdown = false;
        var appThread = new Thread(() =>
        {
            // ReSharper disable once AccessToModifiedClosure
            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            while (!appShutdown)
                // ReSharper disable once AccessToDisposedClosure
                if (!beatmapRunner.ProcessFrame((float) audioPlayer.Position))
                    Thread.Yield();
        });
        appThread.Start();
        
        audioPlayer.Play();
        
        // Enter the render loop
        while (!renderer.Window.ShouldClose)
            if (!renderer.ProcessFrame())
                Thread.Yield();
        
        // When renderer exits, we'll shut down the services
        appShutdown = true;
        
        // Wait for the app thread to finish
        appThread.Join();
    }

    public IAppSettings AppSettings { get; } = appSettings;

    public void ConfigureLogging(ILoggingBuilder loggingBuilder)
        => loggingBuilder.AddConsole();

    public IResourceManager? CreateResourceManager(IServiceProvider serviceProvider)
        => null;

    public IWindowManager CreateWindowManager(IServiceProvider serviceProvider)
        => new DesktopWindowManager();

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
        => new DesktopMediaProvider(beatmapPath);
}