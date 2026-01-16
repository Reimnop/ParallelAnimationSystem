using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Windowing;

public interface IWindowManager
{
    IWindow CreateWindow(string title, Vector2i size, IGraphicsApiSettings graphicsApiSettings);
    
    protected static Exception GetUnsupportedGraphicsApiException(string apiName)
        => new NotSupportedException($"The graphics API '{apiName}' is not supported by this window manager");
}