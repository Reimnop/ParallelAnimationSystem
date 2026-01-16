using OpenTK.Windowing.GraphicsLibraryFramework;
using ParallelAnimationSystem.Windowing.OpenGL;

namespace ParallelAnimationSystem.FFmpeg;

public class FFmpegWindowManager : IOpenGLWindowManager, IDisposable
{
    private readonly FFmpegWindowSettings settings;

    public FFmpegWindowManager(FFmpegWindowSettings settings)
    {
        this.settings = settings;
        
        GLFW.Init();
    }
    
    public IOpenGLWindow CreateWindow(string title, OpenGLSettings glSettings)
        => new FFmpegWindow(title, settings, glSettings);

    public IntPtr GetProcAddress(string procName)
        => GLFW.GetProcAddress(procName);

    public void Dispose()
        => GLFW.Terminate();
}