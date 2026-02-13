using System.Numerics;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ParallelAnimationSystem.DebugStuff;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Desktop.DebugStuff;

public class ImGuiPlatformBackend : IImGuiPlatformBackend, IDisposable
{
    public event EventHandler<IImGuiPlatformBackend.TextInputEventArgs>? TextInput;
    public event EventHandler<IImGuiPlatformBackend.KeyEventArgs>? Key;
    public event EventHandler<IImGuiPlatformBackend.MouseButtonEventArgs>? MouseButton;
    public event EventHandler<IImGuiPlatformBackend.MousePositionEventArgs>? MousePosition;
    public event EventHandler<IImGuiPlatformBackend.MouseWheelEventArgs>? MouseWheel;
    public event EventHandler<IImGuiPlatformBackend.UpdateFrameEventArgs>? UpdateFrame;
    
    private readonly DesktopWindow window;

    // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
    private readonly GLFWCallbacks.ScrollCallback scrollCallback;
    private readonly GLFWCallbacks.CharCallback charCallback;
    private readonly GLFWCallbacks.KeyCallback keyCallback;
    private readonly GLFWCallbacks.MouseButtonCallback mouseButtonCallback;
    private readonly GLFWCallbacks.CursorPosCallback cursorPosCallback;
    // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable
    
    private double previousTime;
    
    public unsafe ImGuiPlatformBackend(IWindow window)
    {
        this.window = (DesktopWindow) window;
        
        this.window.EventsPolled += OnWindowEventsPolled;

        scrollCallback = OnScrollCallback;
        charCallback = OnCharCallback;
        keyCallback = OnKeyCallback;
        mouseButtonCallback = OnMouseButtonCallback;
        cursorPosCallback = OnCursorPosCallback;
        
        var windowPtr = this.window.Handle;
        GLFW.SetScrollCallback(windowPtr, scrollCallback);
        GLFW.SetCharCallback(windowPtr, charCallback);
        GLFW.SetKeyCallback(windowPtr, keyCallback);
        GLFW.SetMouseButtonCallback(windowPtr, mouseButtonCallback);
        GLFW.SetCursorPosCallback(windowPtr, cursorPosCallback);
    }
    
    public unsafe void Dispose()
    {
        var windowPtr = window.Handle;
        GLFW.SetScrollCallback(windowPtr, null);
        GLFW.SetCharCallback(windowPtr, null);
        GLFW.SetKeyCallback(windowPtr, null);
        GLFW.SetMouseButtonCallback(windowPtr, null);
        GLFW.SetCursorPosCallback(windowPtr, null);
        
        window.EventsPolled -= OnWindowEventsPolled;
    }

    private unsafe void OnScrollCallback(Window* windowPtr, double offsetX, double offsetY)
    {
        MouseWheel?.Invoke(this, new IImGuiPlatformBackend.MouseWheelEventArgs(
            new Vector2((float) offsetX, (float) offsetY)));
    }
    
    private unsafe void OnCharCallback(Window* windowPtr, uint codepoint)
    {
        TextInput?.Invoke(this, new IImGuiPlatformBackend.TextInputEventArgs((char) codepoint));
    }
    
    private unsafe void OnKeyCallback(Window* windowPtr, Keys key, int scancode, InputAction action, KeyModifiers modifiers)
    {
        var imGuiKey = TranslateKey(key);
        if (imGuiKey != ImGuiKey.None)
        {
            Key?.Invoke(this, 
                new IImGuiPlatformBackend.KeyEventArgs(imGuiKey, action == InputAction.Press));
            Key?.Invoke(this, 
                new IImGuiPlatformBackend.KeyEventArgs(ImGuiKey.ModCtrl, modifiers.HasFlag(KeyModifiers.Control)));
            Key?.Invoke(this, 
                new IImGuiPlatformBackend.KeyEventArgs(ImGuiKey.ModShift,  modifiers.HasFlag(KeyModifiers.Shift)));
            Key?.Invoke(this,
                new IImGuiPlatformBackend.KeyEventArgs(ImGuiKey.ModAlt, modifiers.HasFlag(KeyModifiers.Alt)));
            Key?.Invoke(this,
                new IImGuiPlatformBackend.KeyEventArgs(ImGuiKey.ModSuper, modifiers.HasFlag(KeyModifiers.Super)));
        }
    }
    
    private unsafe void OnMouseButtonCallback(Window* windowPtr, MouseButton button, InputAction action, KeyModifiers modifiers)
    {
        var imGuiButton = TranslateMouseButton(button);
        
        if (imGuiButton.HasValue)
            MouseButton?.Invoke(this,
                new IImGuiPlatformBackend.MouseButtonEventArgs(imGuiButton.Value, action == InputAction.Press));
    }

    private unsafe void OnCursorPosCallback(Window* windowPtr, double x, double y)
    {
        MousePosition?.Invoke(this,
            new IImGuiPlatformBackend.MousePositionEventArgs(new Vector2((float) x, (float) y)));
    }

    private unsafe void OnWindowEventsPolled(object? sender, EventArgs e)
    {
        var time = GLFW.GetTime();
        var deltaTime = time - previousTime;
        previousTime = time;
        
        var windowPtr = window.Handle;
        
        GLFW.GetFramebufferSize(windowPtr, out var displayWidth, out var displayHeight);

        UpdateFrame?.Invoke(this, new IImGuiPlatformBackend.UpdateFrameEventArgs(
            (float) deltaTime,
            new Vector2(displayWidth, displayHeight)));
    }

    private static ImGuiMouseButton? TranslateMouseButton(MouseButton button)
    {
        return button switch
        {
            OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left => ImGuiMouseButton.Left,
            OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Middle => ImGuiMouseButton.Middle,
            OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Right => ImGuiMouseButton.Right,
            _ => null,
        };
    }

    private static ImGuiKey TranslateKey(Keys key)
    {
        if (key >= Keys.D0 && key <= Keys.D9)
            return key - Keys.D0 + ImGuiKey._0;

        if (key >= Keys.A && key <= Keys.Z)
            return key - Keys.A + ImGuiKey.A;

        if (key >= Keys.KeyPad0 && key <= Keys.KeyPad9)
            return key - Keys.KeyPad0 + ImGuiKey.Keypad0;

        if (key >= Keys.F1 && key <= Keys.F24)
            return key - Keys.F1 + ImGuiKey.F24;

        switch (key)
        {
            case Keys.Tab: return ImGuiKey.Tab;
            case Keys.Left: return ImGuiKey.LeftArrow;
            case Keys.Right: return ImGuiKey.RightArrow;
            case Keys.Up: return ImGuiKey.UpArrow;
            case Keys.Down: return ImGuiKey.DownArrow;
            case Keys.PageUp: return ImGuiKey.PageUp;
            case Keys.PageDown: return ImGuiKey.PageDown;
            case Keys.Home: return ImGuiKey.Home;
            case Keys.End: return ImGuiKey.End;
            case Keys.Insert: return ImGuiKey.Insert;
            case Keys.Delete: return ImGuiKey.Delete;
            case Keys.Backspace: return ImGuiKey.Backspace;
            case Keys.Space: return ImGuiKey.Space;
            case Keys.Enter: return ImGuiKey.Enter;
            case Keys.Escape: return ImGuiKey.Escape;
            case Keys.Apostrophe: return ImGuiKey.Apostrophe;
            case Keys.Comma: return ImGuiKey.Comma;
            case Keys.Minus: return ImGuiKey.Minus;
            case Keys.Period: return ImGuiKey.Period;
            case Keys.Slash: return ImGuiKey.Slash;
            case Keys.Semicolon: return ImGuiKey.Semicolon;
            case Keys.Equal: return ImGuiKey.Equal;
            case Keys.LeftBracket: return ImGuiKey.LeftBracket;
            case Keys.Backslash: return ImGuiKey.Backslash;
            case Keys.RightBracket: return ImGuiKey.RightBracket;
            case Keys.GraveAccent: return ImGuiKey.GraveAccent;
            case Keys.CapsLock: return ImGuiKey.CapsLock;
            case Keys.ScrollLock: return ImGuiKey.ScrollLock;
            case Keys.NumLock: return ImGuiKey.NumLock;
            case Keys.PrintScreen: return ImGuiKey.PrintScreen;
            case Keys.Pause: return ImGuiKey.Pause;
            case Keys.KeyPadDecimal: return ImGuiKey.KeypadDecimal;
            case Keys.KeyPadDivide: return ImGuiKey.KeypadDivide;
            case Keys.KeyPadMultiply: return ImGuiKey.KeypadMultiply;
            case Keys.KeyPadSubtract: return ImGuiKey.KeypadSubtract;
            case Keys.KeyPadAdd: return ImGuiKey.KeypadAdd;
            case Keys.KeyPadEnter: return ImGuiKey.KeypadEnter;
            case Keys.KeyPadEqual: return ImGuiKey.KeypadEqual;
            case Keys.LeftShift: return ImGuiKey.LeftShift;
            case Keys.LeftControl: return ImGuiKey.LeftCtrl;
            case Keys.LeftAlt: return ImGuiKey.LeftAlt;
            case Keys.LeftSuper: return ImGuiKey.LeftSuper;
            case Keys.RightShift: return ImGuiKey.RightShift;
            case Keys.RightControl: return ImGuiKey.RightCtrl;
            case Keys.RightAlt: return ImGuiKey.RightAlt;
            case Keys.RightSuper: return ImGuiKey.RightSuper;
            case Keys.Menu: return ImGuiKey.Menu;
            default: return ImGuiKey.None;
        }
    }
}