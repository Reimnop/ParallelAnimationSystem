using ImGuiNET;

namespace ParallelAnimationSystem.DebugStuff;

public interface IImGuiRendererBackend
{
    public delegate ImDrawDataPtr RenderFrameCallback();

    RenderFrameCallback? RenderFrame { get; set; }
}