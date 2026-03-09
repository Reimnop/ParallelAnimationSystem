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
    public static void Start(bool enablePostProcessing, bool enableTextRendering)
    {
        if (app is not null)
            throw new InvalidOperationException("App already started, call shutdown first");

        var appSettings = new AppSettings
        {
            AspectRatio = null,
            EnablePostProcessing = enablePostProcessing,
            EnableTextRendering = enableTextRendering,
        };

        var services = new ServiceCollection();
        
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddProvider(new WasmLoggerProvider());
        });
        
        services.AddPAS(builder =>
        {
            builder.UseAppSettings(appSettings);
            builder.UseWindow<WasmWindow>();
            builder.UseRenderQueue<RenderQueue>();
            builder.UseOpenGLESRenderer();
        });

        var sp = services.BuildServiceProvider();
        app = new WasmApp(sp);
    }

    [UnmanagedCallersOnly(EntryPoint = "main_shutdown")]
    public static void Shutdown()
    {
        app?.Dispose();
        app = null;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "main_getApp")]
    public static IntPtr GetApp()
    {
        if (app is null)
            return IntPtr.Zero;

        return InteropHelper.ObjectToIntPtr(app);
    }
}