using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Windowing;

public interface IWindow
{
    string Title { get; set; }
    Vector2i FramebufferSize { get; }
    bool ShouldClose { get; }

    void PollEvents();
    void SetSwapInterval(int interval);
    
    void Close();
}