using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem;

public interface IStartup
{
    IAppSettings AppSettings { get; }
    
    void ConfigureLogging(ILoggingBuilder loggingBuilder);
    IResourceManager? CreateResourceManager(IServiceProvider serviceProvider);
    IRenderer CreateRenderer(IServiceProvider serviceProvider);
    IMediaProvider CreateMediaProvider(IServiceProvider serviceProvider);
}