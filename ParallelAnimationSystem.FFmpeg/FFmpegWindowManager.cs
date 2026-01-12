using ParallelAnimationSystem.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.FFmpeg;

public class FFmpegWindowManager : IWindowManager, IDisposable
{
    public FFmpegWindowManager()
        => GLFW.Init();
    
    public IWindow CreateWindow(string title, Vector2i size, GLContextSettings glContextSettings)
        => new FFmpegWindow(title, size, glContextSettings);

    public void PollEvents()
        => GLFW.PollEvents();

    public IntPtr GetProcAddress(string procName)
        => GLFW.GetProcAddress(procName);

    public void Dispose()
        => GLFW.Terminate();
}