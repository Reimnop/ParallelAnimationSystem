using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem.Wasm;

public class WasmApp : IDisposable
{
    public BeatmapService BeatmapService { get; }

    private readonly ServiceProvider sp;
    
    private readonly AppCore appCore;
    private readonly IServiceScope renderScope;
    private readonly IDrawList drawList;
    private readonly IRenderer renderer;
    
    public WasmApp(ServiceProvider sp)
    {
        this.sp = sp;
        
        BeatmapService = sp.GetRequiredService<BeatmapService>();
        
        appCore = sp.GetRequiredService<AppCore>();
        
        renderScope = sp.CreateScope();
        renderer = renderScope.ServiceProvider.GetRequiredService<IRenderer>();
        
        var renderingFactory = sp.GetRequiredService<IRenderingFactory>();
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
        renderScope.Dispose();
        sp.Dispose();
    }
}