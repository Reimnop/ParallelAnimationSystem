using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Platform.OpenGL;
using ParallelAnimationSystem.Rendering.OpenGLES;
using ParallelAnimationSystem.Resources.Raw;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropMain
{
    private static WasmApp? app;
    
    [UnmanagedCallersOnly(EntryPoint = "main_start")]
    public static void Start(bool enablePostProcessing, bool enableTextRendering)
    {
        if (app is not null)
            throw new InvalidOperationException("App already started, call shutdown first");

        var services = new ServiceCollection();
        
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddProvider(new WasmLoggerProvider());
        });
        
        services.AddPAS(x => x.AddRawResources())
            .UseOpenGLESRenderer();

        services.AddScoped<IOpenGLSurface, WasmSurface>();

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