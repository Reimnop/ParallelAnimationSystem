using System.Numerics;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ParallelAnimationSystem.DebugUI;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Desktop;

public class ImGuiPlatformBackend : IImGuiPlatformBackend, IDisposable
{
    private readonly ImGuiBackend backend;
    private readonly DesktopWindow window;
    private readonly AppSettings appSettings;

    // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
    private readonly GLFWCallbacks.ScrollCallback scrollCallback;
    private readonly GLFWCallbacks.CharCallback charCallback;
    private readonly GLFWCallbacks.KeyCallback keyCallback;
    private readonly GLFWCallbacks.MouseButtonCallback mouseButtonCallback;
    // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable

    private readonly ImGuiIOData ioData = new();

    private double scrollOffsetX;
    private double scrollOffsetY;
    private double previousTime;
    
    public unsafe ImGuiPlatformBackend(ImGuiBackend backend, IWindow window, AppSettings appSettings)
    {
        this.backend = backend;
        this.window = (DesktopWindow) window;
        this.appSettings = appSettings;
        
        this.window.EventsPolled += OnWindowEventsPolled;

        scrollCallback = OnScrollCallback;
        charCallback = OnCharCallback;
        keyCallback = OnKeyCallback;
        mouseButtonCallback = OnMouseButtonCallback;
        
        var windowPtr = this.window.Handle;
        GLFW.SetScrollCallback(windowPtr, scrollCallback);
        GLFW.SetCharCallback(windowPtr, charCallback);
        GLFW.SetKeyCallback(windowPtr, keyCallback);
        GLFW.SetMouseButtonCallback(windowPtr, mouseButtonCallback);
    }
    
    public unsafe void Dispose()
    {
        var windowPtr = window.Handle;
        GLFW.SetScrollCallback(windowPtr, null);
        GLFW.SetCharCallback(windowPtr, null);
        GLFW.SetKeyCallback(windowPtr, null);
        GLFW.SetMouseButtonCallback(windowPtr, null);
        
        window.EventsPolled -= OnWindowEventsPolled;
    }

    private unsafe void OnScrollCallback(Window* windowPtr, double offsetX, double offsetY)
    {
        scrollOffsetX += offsetX;
        scrollOffsetY += offsetY;
    }
    
    private unsafe void OnCharCallback(Window* windowPtr, uint codepoint)
    {
        backend.AddTextInput((char) codepoint);
    }
    
    private unsafe void OnKeyCallback(Window* windowPtr, Keys key, int scancode, InputAction action, KeyModifiers modifiers)
    {
        var imGuiKey = TranslateKey(key);
        if (imGuiKey != ImGuiKey.None)
        {
            backend.AddKeyEvent(imGuiKey, action == InputAction.Press);
            backend.AddKeyEvent(ImGuiKey.ModCtrl, modifiers.HasFlag(KeyModifiers.Control));
            backend.AddKeyEvent(ImGuiKey.ModShift, modifiers.HasFlag(KeyModifiers.Shift));
            backend.AddKeyEvent(ImGuiKey.ModAlt, modifiers.HasFlag(KeyModifiers.Alt));
            backend.AddKeyEvent(ImGuiKey.ModSuper, modifiers.HasFlag(KeyModifiers.Super));
        }
    }
    
    private unsafe void OnMouseButtonCallback(Window* windowPtr, MouseButton button, InputAction action, KeyModifiers modifiers)
    {
        ioData.MouseDown[(int) button] = action == InputAction.Press;
    }

    private unsafe void OnWindowEventsPolled(object? sender, EventArgs e)
    {
        var time = GLFW.GetTime();
        var deltaTime = time - previousTime;
        previousTime = time;
        
        var windowPtr = window.Handle;
        
        GLFW.GetFramebufferSize(windowPtr, out var displayWidth, out var displayHeight);
        
        var actualWidth = displayWidth;
        var actualHeight = displayHeight;
        if (appSettings.AspectRatio.HasValue)
        {
            var targetAspectRatio = appSettings.AspectRatio.Value;
            var screenAspectRatio = displayWidth / (float) displayHeight;
            if (targetAspectRatio < screenAspectRatio)
            {
                actualWidth = (int) (displayHeight * targetAspectRatio);
            }
            else
            {
                actualHeight = (int) (displayWidth / targetAspectRatio);
            }
        }
        
        var offsetX = (displayWidth - actualWidth) / 2;
        var offsetY = (displayHeight - actualHeight) / 2;
        
        GLFW.GetCursorPos(windowPtr, out var mouseX, out var mouseY);
        
        ioData.DeltaTime = (float) deltaTime;
        ioData.DisplaySize = new Vector2(actualWidth, actualHeight);
        ioData.MousePos = new Vector2((float) mouseX - offsetX, (float) mouseY - offsetY);
        ioData.MouseWheel = (float) scrollOffsetY;
        ioData.MouseWheelH = (float) scrollOffsetX;
        
        scrollOffsetX = 0;
        scrollOffsetY = 0;
        
        backend.UpdateFrame(ioData);
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