using OpenTK.Graphics.Egl;
using OpenTK.Mathematics;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Wasm;

public class WasmWindowManager : IWindowManager, IDisposable
{
    private readonly IntPtr display;
    
    public WasmWindowManager()
    {
        display = Egl.GetDisplay(IntPtr.Zero);
        Egl.Initialize(display, out _, out _);
    }

    public IWindow CreateWindow(string title, Vector2i size, GLContextSettings glContextSettings)
    {
        if (!glContextSettings.ES)
            throw new NotSupportedException("Only OpenGL ES is supported in WebAssembly");
        
        return new WasmWindow(display, title, glContextSettings);
    }

    public IntPtr GetProcAddress(string procName)
        => Egl.GetProcAddress(procName);

    public void Dispose()
    {
        Egl.Terminate(display);
    }
}