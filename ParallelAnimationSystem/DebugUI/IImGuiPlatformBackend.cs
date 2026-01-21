using System.Numerics;
using ImGuiNET;

namespace ParallelAnimationSystem.DebugUI;

public interface IImGuiPlatformBackend
{
    public record struct TextInputEventArgs(char Codepoint);
    public record struct KeyEventArgs(ImGuiKey Key, bool Down);
    public record struct MouseButtonEventArgs(ImGuiMouseButton MouseButton, bool Down);
    public record struct MousePositionEventArgs(Vector2 Position);
    public record struct MouseWheelEventArgs(Vector2 Delta);

    public record struct UpdateFrameEventArgs(float Delta, Vector2 DisplaySize);
    
    event EventHandler<TextInputEventArgs> TextInput;
    event EventHandler<KeyEventArgs> Key;
    event EventHandler<MouseButtonEventArgs> MouseButton;
    event EventHandler<MousePositionEventArgs> MousePosition;
    event EventHandler<MouseWheelEventArgs> MouseWheel;
    event EventHandler<UpdateFrameEventArgs> UpdateFrame;
    
}