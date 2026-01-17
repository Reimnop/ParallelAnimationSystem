using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering;
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

        var sp = services.BuildServiceProvider();
        var appCore = sp.InitializeAppCore();
        var renderer = sp.InitializeRenderer();
        
        var renderingFactory = sp.GetRequiredService<IRenderingFactory>();
        var drawList = renderingFactory.CreateDrawList();

        app = new WasmApp
        {
            ServiceProvider = sp,
            AppCore = appCore,
            Renderer = renderer,
            DrawList = drawList
        };
    }

    [UnmanagedCallersOnly(EntryPoint = "processFrame")]
    public static void ProcessFrame(float time)
    {
        if (app is null)
            throw new InvalidOperationException("App not initialized");
        
        var appCore = app.AppCore;
        var renderer = app.Renderer;
        var drawList = app.DrawList;

        drawList.Clear();
        appCore.ProcessFrame(time, drawList);
        renderer.ProcessFrame(drawList);
    }

    [UnmanagedCallersOnly(EntryPoint = "dispose")]
    public static void Dispose()
    {
        app?.Dispose();
        app = null;
    }
}