using Android.Views;
using MattiasCibien.Extensions.Logging.Logcat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Rendering.OpenGLES;
using IWindowManager = ParallelAnimationSystem.Windowing.IWindowManager;

namespace ParallelAnimationSystem.Android;

public class AndroidStartup(IAppSettings appSettings, string beatmapData, BeatmapFormat beatmapFormat, ISurfaceHolder holder) : IStartup
{
    private const string LogcatTag = "ParallelAnimationSystem";

    public IAppSettings AppSettings { get; } = appSettings;

    public void ConfigureLogging(ILoggingBuilder loggingBuilder)
        => loggingBuilder.AddLogcat(LogcatTag);

    public IResourceManager? CreateResourceManager(IServiceProvider serviceProvider)
        => null;

    public IWindowManager CreateWindowManager(IServiceProvider serviceProvider)
        => new AndroidWindowManager(holder);

    public IRenderer CreateRenderer(IServiceProvider serviceProvider)
        => new Renderer(
            serviceProvider.GetRequiredService<IAppSettings>(),
            serviceProvider.GetRequiredService<IWindowManager>(),
            serviceProvider.GetRequiredService<IResourceManager>(),
            serviceProvider.GetRequiredService<ILogger<Renderer>>());

    public IMediaProvider CreateMediaProvider(IServiceProvider serviceProvider)
        => new AndroidMediaProvider(beatmapData, beatmapFormat);
}