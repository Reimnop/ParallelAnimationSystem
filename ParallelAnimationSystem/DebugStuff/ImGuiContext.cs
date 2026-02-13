using ImGuiNET;

namespace ParallelAnimationSystem.DebugStuff;

public class ImGuiContext : IDisposable
{
    public ImGuiIOPtr IO { get; }

    public ImGuiContext()
    {
        ImGui.CreateContext();
        
        IO = ImGui.GetIO();
        IO.Fonts.AddFontDefault();
    }
    
    public void Dispose()
    {
        ImGui.DestroyContext();
    }
}