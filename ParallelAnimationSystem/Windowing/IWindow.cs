using OpenTK.Mathematics;

namespace ParallelAnimationSystem.Windowing;

public delegate bool AnimationFrameCallback(double deltaTime);

public interface IWindow
{
    string Title { get; set; }
    
    Vector2i FramebufferSize { get; }
    
    bool ShouldClose { get; }
    
    void SetSwapInterval(int interval);
    void RequestAnimationFrame(AnimationFrameCallback callback);
    
    void Close();
}