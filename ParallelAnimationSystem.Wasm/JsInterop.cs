using System.Runtime.InteropServices;
using ParallelAnimationSystem.Core;

namespace ParallelAnimationSystem.Wasm;

public static class JsInterop
{
    private static App? app;
    
    [UnmanagedCallersOnly(EntryPoint = "initialize")]
    public static unsafe void Initialize(
        long seed, 
        bool enablePostProcessing, 
        bool enableTextRendering,
        byte* beatmapDataPtr,
        int beatmapFormat)
    {
        var beatmapData = Marshal.PtrToStringUTF8((IntPtr) beatmapDataPtr);
        if (beatmapData is null)
            throw new InvalidOperationException("Beatmap data is null");
        
        var appSettings = new WasmAppSettings(unchecked((ulong) seed), enablePostProcessing, enableTextRendering);
        var startup = new WasmStartup(appSettings, beatmapData, (BeatmapFormat) beatmapFormat);
        app = startup.InitializeApp();
    }

    [UnmanagedCallersOnly(EntryPoint = "processFrame")]
    public static void ProcessFrame(float time)
    {
        if (app is null)
            throw new InvalidOperationException("App not initialized");
        
        var (_, renderer, beatmapRunner) = app;
        beatmapRunner.ProcessFrame(time);
        renderer.ProcessFrame();
    }

    [UnmanagedCallersOnly(EntryPoint = "dispose")]
    public static void Dispose()
    {
        app?.Dispose();
        app = null;
    }
}