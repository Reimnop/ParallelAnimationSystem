using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core.Service;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropApp
{
    [UnmanagedCallersOnly(EntryPoint = "app_getBeatmapDataPointer")]
    public static IntPtr GetBeatmapDataPointer(IntPtr ptr)
    {
        var app = InteropHelper.IntPtrToObject<WasmApp>(ptr);
        var beatmapService = app.ServiceProvider.GetRequiredService<BeatmapService>();
        return InteropHelper.ObjectToIntPtr(beatmapService.BeatmapData);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "app_processFrame")]
    public static void ProcessFrame(IntPtr ptr, float time)
    {
        var app = InteropHelper.IntPtrToObject<WasmApp>(ptr);
        app.ProcessFrame(time);
    }
}