using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem.Wasm;

public class WasmApp : IDisposable
{
    public RandomSeedService RandomSeedService { get; }
    public BeatmapService BeatmapService { get; }

    private readonly ServiceProvider sp;
    
    private readonly IServiceScope scope;
    private readonly AppDirector appDirector;
    private readonly IRenderer renderer;

    private readonly RenderQueue renderQueue;

    public WasmApp(ServiceProvider sp)
    {
        this.sp = sp;
        
        scope = sp.CreateScope();
        
        RandomSeedService = scope.ServiceProvider.GetRequiredService<RandomSeedService>();
        BeatmapService = scope.ServiceProvider.GetRequiredService<BeatmapService>();
        appDirector = scope.ServiceProvider.GetRequiredService<AppDirector>();
        renderer = scope.ServiceProvider.GetRequiredService<IRenderer>();
        renderQueue = scope.ServiceProvider.GetRequiredService<RenderQueue>();
    }
    
    public void ProcessFrame(float time)
    {
        appDirector.PopulateRenderQueueDrawList(time);
        renderQueue.FinishFrame();
        renderQueue.FlushFrame(renderer);
    }

    public void Dispose()
    {
        scope.Dispose();
        sp.Dispose();
    }
}