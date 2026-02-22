namespace ParallelAnimationSystem.DebugStuff;

public interface IDebugHandler
{
    void UpdateFrame(ImGuiContext context);
    void RenderFrame(ImGuiContext context);
}