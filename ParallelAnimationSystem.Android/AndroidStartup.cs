using MattiasCibien.Extensions.Logging.Logcat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Rendering.OpenGLES;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Android;

public class AndroidStartup(IAppSettings appSettings) : IStartup
{
    private const string LogcatTag = "ParallelAnimationSystem";

    public IAppSettings AppSettings { get; } = appSettings;

    public void ConfigureLogging(ILoggingBuilder loggingBuilder)
        => loggingBuilder.AddLogcat(LogcatTag);

    public IResourceManager? CreateResourceManager(IServiceProvider serviceProvider)
        => null;

    public IWindowManager CreateWindowManager(IServiceProvider serviceProvider)
        => new AndroidWindowManager();

    public IRenderer CreateRenderer(IServiceProvider serviceProvider)
        => new Renderer(
            serviceProvider.GetRequiredService<IAppSettings>(),
            serviceProvider.GetRequiredService<IWindowManager>(),
            serviceProvider.GetRequiredService<IResourceManager>(),
            serviceProvider.GetRequiredService<ILogger<Renderer>>());

    public IMediaProvider CreateMediaProvider(IServiceProvider serviceProvider)
        => new AndroidMediaProvider();
}