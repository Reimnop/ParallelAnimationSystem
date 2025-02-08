using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Rendering.OpenGLES;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Wasm;

public class WasmStartup(WasmAppSettings appSettings, string beatmapData, BeatmapFormat beatmapFormat) : IStartup
{
    public IAppSettings AppSettings { get; } = appSettings;
    
    public void ConfigureLogging(ILoggingBuilder loggingBuilder)
        => loggingBuilder.AddProvider(new WasmLoggerProvider());

    public IResourceManager CreateResourceManager(IServiceProvider serviceProvider)
        => new EmbeddedResourceManager(typeof(WasmStartup).Assembly); // Provide decompressed font files, as System.IO.Compression isn't available in WASM

    public IWindowManager CreateWindowManager(IServiceProvider serviceProvider)
        => new WasmWindowManager();

    public IRenderer CreateRenderer(IServiceProvider serviceProvider)
        => new Renderer(
            serviceProvider.GetRequiredService<IAppSettings>(),
            serviceProvider.GetRequiredService<IWindowManager>(),
            serviceProvider.GetRequiredService<IResourceManager>(),
            serviceProvider.GetRequiredService<ILogger<Renderer>>());

    public IMediaProvider CreateMediaProvider(IServiceProvider serviceProvider)
        => new WasmMediaProvider(beatmapData, beatmapFormat);
}