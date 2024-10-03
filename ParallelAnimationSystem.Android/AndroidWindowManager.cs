using OpenTK.Mathematics;
using ParallelAnimationSystem.Windowing;
using static SDL.SDL3;

namespace ParallelAnimationSystem.Android;

public class AndroidWindowManager : IWindowManager, IDisposable
{
    public IWindow CreateWindow(string title, Vector2i size, GLContextSettings glContextSettings)
    {
        if (!glContextSettings.ES)
            throw new NotSupportedException("Desktop OpenGL is not supported on Android, use OpenGL ES instead");
        
        return new AndroidWindow(title, size, glContextSettings);
    }

    public IntPtr GetProcAddress(string procName)
        => SDL_GL_GetProcAddress(procName);

    public void Dispose()
    {
        SDL_Quit();
    }
}