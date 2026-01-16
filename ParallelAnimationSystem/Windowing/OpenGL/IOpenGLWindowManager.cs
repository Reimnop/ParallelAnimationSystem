using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Windowing.OpenGL;

public interface IOpenGLWindowManager : IWindowManager
{
    IWindow IWindowManager.CreateWindow(string title, Vector2i size, IGraphicsApiSettings graphicsApiSettings)
    {
        if (graphicsApiSettings is not OpenGLSettings glSettings)
            throw GetUnsupportedGraphicsApiException("OpenGL");
        
        return CreateWindow(title, size, glSettings);
    }
    
    IOpenGLWindow CreateWindow(string title, Vector2i size, OpenGLSettings glSettings);

    IntPtr GetProcAddress(string procName);
}