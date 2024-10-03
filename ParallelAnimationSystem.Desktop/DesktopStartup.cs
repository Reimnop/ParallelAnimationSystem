using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Desktop;

public class DesktopStartup(DesktopAppSettings appSettings, string beatmapPath, string audioPath, RenderingBackend backend) : IStartup
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Options))]
    public static void ConsumeOptions(Options options)
    {
        var appSettings = new DesktopAppSettings(
            (options.VSync ?? true) ? 1 : 0,
            options.WorkerCount,
            options.Seed < 0
                ? (ulong) DateTimeOffset.Now.ToUnixTimeMilliseconds()
                : (ulong) options.Seed,
            options.EnableTextRendering
        );
        
        var startup = new DesktopStartup(appSettings, options.BeatmapPath, options.AudioPath, options.Backend ?? RenderingBackend.OpenGL);
        using var app = startup.InitializeApp();
        using var audioPlayer = AudioPlayer.Load(options.AudioPath);
        var baseFrequency = audioPlayer.Frequency;
        audioPlayer.Frequency = baseFrequency * options.Speed;
        
        var beatmapRunner = app.BeatmapRunner;
        var renderer = app.Renderer;
        
        var appShutdown = false;
        var appThread = new Thread(() =>
        {
            // ReSharper disable once AccessToModifiedClosure
            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            while (!appShutdown)
                if (!beatmapRunner.ProcessFrame(audioPlayer.Position))
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