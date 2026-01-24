namespace ParallelAnimationSystem.DebugUI;

public interface IDebugHandler
{
    void UpdateFrame(ImGuiContext context);
    void RenderFrame(ImGuiContext context);
}