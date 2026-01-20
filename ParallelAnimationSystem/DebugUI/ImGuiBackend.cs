using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace ParallelAnimationSystem.DebugUI;

public class ImGuiBackend : IDisposable
{
    public ImGuiBackend(ILogger<ImGuiBackend> logger)
    {
        logger.LogInformation("Creating context");
        ImGui.CreateContext();
        
        logger.LogInformation("Initializing IO");
        var io = ImGui.GetIO();
        io.Fonts.AddFontDefault();
    }
    
    public void Dispose()
    {
        ImGui.DestroyContext();
    }

    public void AddTextInput(char c)
    {
        var io = ImGui.GetIO();
        io.AddInputCharacter(c);
    }

    public void AddKeyEvent(ImGuiKey key, bool down)
    {
        var io = ImGui.GetIO();
        io.AddKeyEvent(key, down);
    }

    public void UpdateFrame(ImGuiIOData ioData)
    {
        var io = ImGui.GetIO();
        io.DeltaTime = ioData.DeltaTime;
        io.DisplaySize = ioData.DisplaySize;
        io.MousePos = ioData.MousePos;
        for (var i = 0; i < io.MouseDown.Count; i++)
            io.MouseDown[i] = ioData.MouseDown[i];
        io.MouseWheel = ioData.MouseWheel;
        io.MouseWheelH = ioData.MouseWheelH;
    }

    public ImDrawDataPtr RenderFrame()
    {
        ImGui.NewFrame();
        
        ImGui.ShowDemoWindow();
        
        // TODO: Call UI render callbacks

        ImGui.Render();
        return ImGui.GetDrawData();
    }
}