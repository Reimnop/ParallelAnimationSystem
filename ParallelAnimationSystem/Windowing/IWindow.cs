using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Windowing;

public interface IWindow
{
    Vector2i FramebufferSize { get; }
    bool ShouldClose { get; }

    void PollEvents();
    
    void Close();
}