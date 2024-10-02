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
            options.Speed,
            options.EnableTextRendering
        );
        
        Startup.StartApp(new DesktopStartup(appSettings, options.BeatmapPath, options.AudioPath, options.Backend ?? RenderingBackend.OpenGL));
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
        => new DesktopMediaProvider(beatmapPath, audioPath);
}