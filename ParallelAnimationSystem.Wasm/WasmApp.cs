using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem.Wasm;

public class WasmApp : IDisposable
{
    public required ServiceProvider ServiceProvider { get; init; }
    public required AppCore AppCore { get; init; }
    public required IRenderer Renderer { get; init; }
    public required IDrawList DrawList { get; init; }
    
    public void ProcessFrame(float time)
    {
        DrawList.Clear();
        AppCore.ProcessFrame(time, DrawList);
        Renderer.ProcessFrame(DrawList);
    }

    public void Dispose()
    {
        ServiceProvider.Dispose();
    }
}