using System.Runtime.InteropServices;
using ParallelAnimationSystem.Core;

namespace ParallelAnimationSystem.Wasm;

public static class JsInterop
{
    private static App? app;
    
    [UnmanagedCallersOnly(EntryPoint = "initialize")]
    public static unsafe bool Initialize(
        long seed, 
        bool enablePostProcessing, 
        bool enableTextRendering,
        byte* beatmapDataPtr,
        int beatmapFormat)
    {
        try
        {
            var beatmapData = Marshal.PtrToStringUTF8((IntPtr) beatmapDataPtr);
            if (beatmapData is null)
                throw new InvalidOperationException("Beatmap data is null");
        
            var appSettings = new WasmAppSettings(unchecked((ulong) seed), enablePostProcessing, enableTextRendering);
            var startup = new WasmStartup(appSettings, beatmapData, (BeatmapFormat) beatmapFormat);
            app = startup.InitializeApp();
            return true;
        }
        catch
        {
            return false;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "processFrame")]
    public static bool ProcessFrame(float time)
    {
        try
        {
            if (app is null)
                throw new InvalidOperationException("App not initialized");
        
            var (_, renderer, beatmapRunner) = app;
            beatmapRunner.ProcessFrame(time);
            renderer.ProcessFrame();
            return true;
        }
        catch
        {
            return false;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "dispose")]
    public static void Dispose()
    {
        app?.Dispose();
        app = null;
    }
}