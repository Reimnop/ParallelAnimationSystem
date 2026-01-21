using ImGuiNET;

namespace ParallelAnimationSystem.DebugUI;

public interface IImGuiRendererBackend
{
    public delegate ImDrawDataPtr RenderFrameCallback();

    RenderFrameCallback? RenderFrame { get; set; }
}