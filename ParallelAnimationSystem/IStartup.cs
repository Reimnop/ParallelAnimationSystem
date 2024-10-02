using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem;

public interface IStartup
{
    IAppSettings AppSettings { get; }
    
    void ConfigureLogging(ILoggingBuilder loggingBuilder);
    IResourceManager? CreateResourceManager(IServiceProvider serviceProvider);
    IWindowManager CreateWindowManager(IServiceProvider serviceProvider);
    IRenderer CreateRenderer(IServiceProvider serviceProvider);
    IMediaProvider CreateMediaProvider(IServiceProvider serviceProvider);
}