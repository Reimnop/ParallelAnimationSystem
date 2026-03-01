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
    private readonly AppCore appCore;
    private readonly IDrawList drawList;
    private readonly IRenderer renderer;
    
    public WasmApp(ServiceProvider sp)
    {
        this.sp = sp;
        
        scope = sp.CreateScope();
        
        RandomSeedService = scope.ServiceProvider.GetRequiredService<RandomSeedService>();
        BeatmapService = scope.ServiceProvider.GetRequiredService<BeatmapService>();
        appCore = scope.ServiceProvider.GetRequiredService<AppCore>();
        renderer = scope.ServiceProvider.GetRequiredService<IRenderer>();
        
        var renderingFactory = scope.ServiceProvider.GetRequiredService<IRenderingFactory>();
        drawList = renderingFactory.CreateDrawList();
    }
    
    public void ProcessFrame(float time)
    {
        drawList.Clear();
        appCore.ProcessFrame(time, drawList);
        renderer.ProcessFrame(drawList);
    }

    public void Dispose()
    {
        scope.Dispose();
        sp.Dispose();
    }
}