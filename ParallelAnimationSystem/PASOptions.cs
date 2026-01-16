using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Util;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem;

public class PASOptions
{
    public required AppSettings AppSettings { get; init; }
    public required List<Func<IResourceSource>> ResourceSourceFactories { get; init; }
    public required ServiceDefinition<IWindowManager> WindowManagerDefinition { get; init; }
    public required ServiceDefinition<IMediaProvider> MediaProviderDefinition { get; init; }
    public required ServiceDefinition<IRenderer> RendererDefinition { get; init; }
    public required ServiceDefinition<IRenderingFactory> RenderingFactoryDefinition { get; init; }
}