using System.Runtime.InteropServices.JavaScript;

namespace ParallelAnimationSystem.Wasm;

public static partial class JsApi
{
    public const string ModuleName = "ParallelAnimationSystem";

    private static App? app;
    
    [JSImport("getBeatmapData", ModuleName)]
    public static partial string GetBeatmapData();
    
    [JSImport("getBeatmapFormat", ModuleName)]
    public static partial string GetBeatmapFormat();
    
    [JSExport]
    public static void Initialize(int seedLow, int seedHigh, bool enablePostProcessing, bool enableTextRendering)
    {
        var seed = unchecked((uint)seedLow) | ((ulong)seedHigh << 32);
        var appSettings = new WasmAppSettings(seed, enablePostProcessing, enableTextRendering);
        var startup = new WasmStartup(appSettings);
        app = startup.InitializeApp();
    }

    [JSExport]
    public static void ProcessFrame(float time)
    {
        if (app is null)
            throw new InvalidOperationException("App not initialized");
        
        var (_, renderer, beatmapRunner) = app;
        beatmapRunner.ProcessFrame(time);
        renderer.ProcessFrame();
    }

    [JSExport]
    public static void Dispose()
    {
        app?.Dispose();
        app = null;
    }
}