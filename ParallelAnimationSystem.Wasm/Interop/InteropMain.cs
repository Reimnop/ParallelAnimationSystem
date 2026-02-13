using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Rendering.OpenGLES;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropMain
{
    private static WasmApp? app;
    
    [UnmanagedCallersOnly(EntryPoint = "main_start")]
    public static unsafe void Start(
        ulong seed, 
        bool enablePostProcessing, 
        bool enableTextRendering,
        byte* beatmapDataPtr,
        int beatmapFormat)
    {
        if (app is not null)
            throw new InvalidOperationException("App already started, call shutdown first");
        
        var beatmapData = Marshal.PtrToStringUTF8((IntPtr) beatmapDataPtr);
        if (beatmapData is null)
            throw new NullReferenceException("Beatmap data is null");

        var appSettings = new AppSettings
        {
            Seed = seed,
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
            builder.UseWindow<WasmWindow>();
            builder.UseMediaProvider<WasmMediaProvider>();
            builder.UseOpenGLESRenderer();
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

    [UnmanagedCallersOnly(EntryPoint = "main_shutdown")]
    public static void Shutdown()
    {
        app?.Dispose();
        app = null;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "main_getAppPointer")]
    public static IntPtr GetAppPointer()
    {
        if (app is null)
            return IntPtr.Zero;
        
        return (IntPtr)GCHandle.Alloc(app);
    }

    [UnmanagedCallersOnly(EntryPoint = "main_releaseAppPointer")]
    public static void ReleaseAppPointer(IntPtr appPointer)
    {
        var handle = GCHandle.FromIntPtr(appPointer);
        handle.Free();
    }
}