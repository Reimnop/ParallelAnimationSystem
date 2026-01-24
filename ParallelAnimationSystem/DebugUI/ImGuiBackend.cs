using ImGuiNET;

namespace ParallelAnimationSystem.DebugUI;

public class ImGuiBackend : IDisposable
{
    private readonly List<IDebugHandler> debugHandlers = [];
    
    private readonly ImGuiContext context;
    private readonly IImGuiPlatformBackend platformBackend;
    private readonly IImGuiRendererBackend rendererBackend;

    public ImGuiBackend(
        ImGuiContext context,
        IImGuiPlatformBackend platformBackend,
        IImGuiRendererBackend rendererBackend)
    {
        this.context = context;
        this.platformBackend = platformBackend;
        this.rendererBackend = rendererBackend;

        this.platformBackend.TextInput += OnTextInput;
        this.platformBackend.Key += OnKey;
        this.platformBackend.MouseButton += OnMouseButton;
        this.platformBackend.MousePosition += OnMousePosition;
        this.platformBackend.MouseWheel += OnMouseWheel;
        this.platformBackend.UpdateFrame += OnUpdateFrame;
        
        this.rendererBackend.RenderFrame = RenderFrame;
    }
    
    public void Dispose()
    {
        platformBackend.TextInput -= OnTextInput;
        platformBackend.Key -= OnKey;
        platformBackend.MouseButton -= OnMouseButton;
        platformBackend.MousePosition -= OnMousePosition;
        platformBackend.MouseWheel -= OnMouseWheel;
        platformBackend.UpdateFrame -= OnUpdateFrame;

        rendererBackend.RenderFrame = null;
    }
    
    public void AddDebugHandler(IDebugHandler handler)
    {
        debugHandlers.Add(handler);
    }
    
    public bool RemoveDebugHandler(IDebugHandler handler)
    {
        return debugHandlers.Remove(handler);
    }
    
    private void OnTextInput(object? sender, IImGuiPlatformBackend.TextInputEventArgs eventArgs)
    {
        var io = context.IO;
        io.AddInputCharacter(eventArgs.Codepoint);
    }

    private void OnKey(object? sender, IImGuiPlatformBackend.KeyEventArgs eventArgs)
    {
        var io = context.IO;
        io.AddKeyEvent(eventArgs.Key, eventArgs.Down);
    }

    private void OnMouseButton(object? sender, IImGuiPlatformBackend.MouseButtonEventArgs eventArgs)
    {
        var io = context.IO;
        io.AddMouseButtonEvent((int) eventArgs.MouseButton, eventArgs.Down);
    }

    private void OnMousePosition(object? sender, IImGuiPlatformBackend.MousePositionEventArgs eventArgs)
    {
        var io = context.IO;
        io.AddMousePosEvent(eventArgs.Position.X, eventArgs.Position.Y);
    }

    private void OnMouseWheel(object? sender, IImGuiPlatformBackend.MouseWheelEventArgs eventArgs)
    {
        var io = context.IO;
        io.AddMouseWheelEvent(eventArgs.Delta.X, eventArgs.Delta.Y);
    }

    private void OnUpdateFrame(object? sender, IImGuiPlatformBackend.UpdateFrameEventArgs eventArgs)
    {
        var io = context.IO;
        io.DeltaTime = eventArgs.Delta;
        io.DisplaySize = eventArgs.DisplaySize;
        
        foreach (var handler in debugHandlers)
            handler.UpdateFrame(context);
    }

    private ImDrawDataPtr RenderFrame()
    {
        ImGui.NewFrame();
        
        foreach (var handler in debugHandlers)
            handler.RenderFrame(context);

        ImGui.Render();
        return ImGui.GetDrawData();
    }
}