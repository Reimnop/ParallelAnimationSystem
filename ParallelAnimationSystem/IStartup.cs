using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem;

public interface IStartup
{
    void ConfigureLogging(ILoggingBuilder loggingBuilder);
    IAppSettings CreateAppSettings();
    IResourceManager CreateResourceManager(IServiceProvider serviceProvider);
    IRenderer CreateRenderer(IServiceProvider serviceProvider);
}