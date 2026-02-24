using System.Runtime.InteropServices;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropApp
{
    [UnmanagedCallersOnly(EntryPoint = "app_getRandomSeedService")]
    public static IntPtr GetRandomSeedService(IntPtr ptr)
    {
        var app = InteropHelper.IntPtrToObject<WasmApp>(ptr);
        return InteropHelper.ObjectToIntPtr(app.RandomSeedService);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "app_getBeatmapService")]
    public static IntPtr GetBeatmapService(IntPtr ptr)
    {
        var app = InteropHelper.IntPtrToObject<WasmApp>(ptr);
        return InteropHelper.ObjectToIntPtr(app.BeatmapService);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "app_processFrame")]
    public static void ProcessFrame(IntPtr ptr, float time)
    {
        var app = InteropHelper.IntPtrToObject<WasmApp>(ptr);
        app.ProcessFrame(time);
    }
}