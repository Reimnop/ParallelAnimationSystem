namespace ParallelAnimationSystem.Windowing.OpenGL;

public interface IOpenGLWindowManager : IWindowManager
{
    IWindow IWindowManager.CreateWindow(string title, IGraphicsApiSettings graphicsApiSettings)
    {
        if (graphicsApiSettings is not OpenGLSettings glSettings)
            throw GetUnsupportedGraphicsApiException("OpenGL");
        
        return CreateWindow(title, glSettings);
    }
    
    IOpenGLWindow CreateWindow(string title, OpenGLSettings glSettings);

    IntPtr GetProcAddress(string procName);
}