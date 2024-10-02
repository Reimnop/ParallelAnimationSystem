using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem;

public record App(IServiceProvider ServiceProvider, IRenderer Renderer, BeatmapRunner BeatmapRunner) : IDisposable
{
    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}