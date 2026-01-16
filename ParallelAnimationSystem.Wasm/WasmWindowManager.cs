using OpenTK.Graphics.Egl;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Windowing.OpenGL;

namespace ParallelAnimationSystem.Wasm;

public class WasmWindowManager : IOpenGLWindowManager, IDisposable
{
    private readonly IntPtr display;
    
    public WasmWindowManager()
    {
        display = Egl.GetDisplay(IntPtr.Zero);
        if (display == IntPtr.Zero)
            throw new InvalidOperationException("Failed to get EGL display");
        
        if (!Egl.Initialize(display, out _, out _))
            throw new InvalidOperationException("Failed to initialize EGL");
    }

    public IOpenGLWindow CreateWindow(string title, Vector2i size, OpenGLSettings glSettings)
        => new WasmWindow(display, title, glSettings);

    public IntPtr GetProcAddress(string procName)
        => Egl.GetProcAddress(procName);

    public void Dispose()
    {
        Egl.Terminate(display);
    }
}