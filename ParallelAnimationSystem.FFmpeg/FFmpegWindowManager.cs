using ParallelAnimationSystem.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ParallelAnimationSystem.Windowing.OpenGL;

namespace ParallelAnimationSystem.FFmpeg;

public class FFmpegWindowManager : IOpenGLWindowManager, IDisposable
{
    public FFmpegWindowManager()
        => GLFW.Init();
    
    public IOpenGLWindow CreateWindow(string title, Vector2i size, OpenGLSettings glSettings)
        => new FFmpegWindow(title, size, glSettings);

    public IntPtr GetProcAddress(string procName)
        => GLFW.GetProcAddress(procName);

    public void Dispose()
        => GLFW.Terminate();
}