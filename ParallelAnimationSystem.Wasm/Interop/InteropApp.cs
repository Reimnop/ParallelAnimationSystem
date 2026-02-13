using System.Runtime.InteropServices;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropApp
{
    [UnmanagedCallersOnly(EntryPoint = "app_processFrame")]
    public static void ProcessFrame(IntPtr ptr, float time)
    {
        var appHandle = GCHandle.FromIntPtr(ptr);
        var app = (WasmApp) appHandle.Target!;
        app.ProcessFrame(time);
    }
}