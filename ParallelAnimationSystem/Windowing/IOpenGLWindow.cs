using OpenTK.Mathematics;

namespace ParallelAnimationSystem.Windowing;

public interface IOpenGLWindow
{
    string Title { get; set; }
    
    Vector2i FramebufferSize { get; }
    
    bool ShouldClose { get; }
    
    void MakeCurrent();
    void SetSwapInterval(int interval);
    void SwapBuffers();
    
    void Close();
}