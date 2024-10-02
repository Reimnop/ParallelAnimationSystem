using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem.Desktop;

public class DesktopStartup(DesktopAppSettings appSettings, string beatmapPath, string audioPath, RenderingBackend backend) : IStartup
{
    public static void ConsumeOptions(Options options)
    {
        var appSettings = new DesktopAppSettings(
            options.WorkerCount,
            options.Seed < 0
                ? (ulong) DateTimeOffset.Now.ToUnixTimeMilliseconds()
                : (ulong) options.Seed,
            options.EnableTextRendering
        );
        
        Startup.StartApp(new DesktopStartup(appSettings, options.BeatmapPath, options.AudioPath, options.Backend));
    }

    public IAppSettings AppSettings { get; } = appSettings;

    public void ConfigureLogging(ILoggingBuilder loggingBuilder)
        => loggingBuilder.AddConsole();

    public IResourceManager? CreateResourceManager(IServiceProvider serviceProvider)
        => null;

    public IRenderer CreateRenderer(IServiceProvider serviceProvider)
        => backend switch
        {
            RenderingBackend.OpenGL => new Rendering.OpenGL.Renderer(
                serviceProvider.GetRequiredService<IResourceManager>(),
                serviceProvider.GetRequiredService<ILogger<Rendering.OpenGL.Renderer>>()),
            RenderingBackend.OpenGLES => new Rendering.OpenGLES.Renderer(
                serviceProvider.GetRequiredService<IResourceManager>(),
                serviceProvider.GetRequiredService<ILogger<Rendering.OpenGLES.Renderer>>()),
            _ => throw new ArgumentOutOfRangeException(nameof(backend), backend, null)
        };

    public IMediaProvider CreateMediaProvider(IServiceProvider serviceProvider)
        => new DesktopMediaProvider(beatmapPath, audioPath);
}