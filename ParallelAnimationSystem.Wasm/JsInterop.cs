using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering.OpenGLES;

namespace ParallelAnimationSystem.Wasm;

public static class JsInterop
{
    private static WasmApp? app;
    
    [UnmanagedCallersOnly(EntryPoint = "initialize")]
    public static unsafe void Initialize(
        long seed, 
        bool enablePostProcessing, 
        bool enableTextRendering,
        byte* beatmapDataPtr,
        int beatmapFormat)
    {
        if (app is not null)
            throw new InvalidOperationException("App already initialized");
        
        var beatmapData = Marshal.PtrToStringUTF8((IntPtr) beatmapDataPtr);
        if (beatmapData is null)
            throw new NullReferenceException("Beatmap data is null");

        var appSettings = new AppSettings
        {
            InitialSize = new Vector2i(1366, 768),
            SwapInterval = 0,
            WorkerCount = -1,
            Seed = unchecked((ulong) seed),
            AspectRatio = null,
            EnablePostProcessing = enablePostProcessing,
            EnableTextRendering = enableTextRendering,
        };

        var services = new ServiceCollection();
        
        services.AddSingleton(new MediaContext
        {
            BeatmapData = beatmapData,
            BeatmapFormat = (BeatmapFormat) beatmapFormat
        });
        
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddProvider(new WasmLoggerProvider());
        });
        
        services.AddPAS(builder =>
        {
            builder.UseAppSettings(appSettings);
            builder.UseWindowManager<WasmWindowManager>();
            builder.UseMediaProvider<WasmMediaProvider>();
            builder.UseOpenGLESRenderer();
            
            // Provide our own font files for WASM
            builder.AddResourceSource(new EmbeddedResourceSource(typeof(WasmApp).Assembly));
        });

        var sp = services.InitializePAS(out var runner, out var renderer);

        app = new WasmApp
        {
            ServiceProvider = sp,
            Runner = runner,
            Renderer = renderer
        };
    }

    [UnmanagedCallersOnly(EntryPoint = "processFrame")]
    public static void ProcessFrame(float time)
    {
        if (app is null)
            throw new InvalidOperationException("App not initialized");
        
        app.Runner.ProcessFrame(time);
        app.Renderer.ProcessFrame();
    }

    [UnmanagedCallersOnly(EntryPoint = "dispose")]
    public static void Dispose()
    {
        app?.Dispose();
        app = null;
    }
}