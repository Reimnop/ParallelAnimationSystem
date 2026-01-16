namespace ParallelAnimationSystem.Windowing;

public interface IWindowManager
{
    IWindow CreateWindow(string title, IGraphicsApiSettings graphicsApiSettings);
    
    protected static Exception GetUnsupportedGraphicsApiException(string apiName)
        => new NotSupportedException($"The graphics API '{apiName}' is not supported by this window manager");
}