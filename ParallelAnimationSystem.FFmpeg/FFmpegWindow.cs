using OpenTK.Graphics.OpenGLES2;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.FFmpeg;

public unsafe class FFmpegWindow : IWindow, IDisposable
{
    public string Title
    {
        get => GLFW.GetWindowTitle(window);
        set => GLFW.SetWindowTitle(window, value);
    }

    public Vector2i FramebufferSize
    {
        get
        {
            GLFW.GetFramebufferSize(window, out var width, out var height);
            return new Vector2i(width, height);
        }
    }
    
    public bool ShouldClose => GLFW.WindowShouldClose(window);

    public FrameData? CurrentFrame { get; private set; }

    private readonly Window* window;

    public FFmpegWindow(string title, Vector2i size, GLContextSettings glContextSettings)
    {
        GLFW.WindowHint(WindowHintClientApi.ClientApi, glContextSettings.ES ? ClientApi.OpenGlEsApi : ClientApi.OpenGlApi);
        if (!glContextSettings.ES)
            GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);
        GLFW.WindowHint(WindowHintInt.ContextVersionMajor, glContextSettings.Version.Major);
        GLFW.WindowHint(WindowHintInt.ContextVersionMinor, glContextSettings.Version.Minor);
        GLFW.WindowHint(WindowHintBool.Visible, false);
        GLFW.WindowHint(WindowHintBool.Resizable, false);
        GLFW.WindowHint(WindowHintBool.Decorated, false);
        
        window = GLFW.CreateWindow(size.X, size.Y, title, null, null);
    }
    
    public void MakeContextCurrent()
    {
        GLFW.MakeContextCurrent(window);
    }

    public void SetSwapInterval(int interval)
    {
        GLFW.MakeContextCurrent(window);
        GLFW.SwapInterval(interval);
    }

    public void RequestAnimationFrame(AnimationFrameCallback callback)
    {
        // GLFW.PollEvents();
        
        GLFW.MakeContextCurrent(window);
        var time = GLFW.GetTime();
        if (callback(time, 0))
        {
            var size = FramebufferSize;
            var frame = new FrameData(size.X, size.Y);
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.ReadPixels(0, 0, size.X, size.Y, PixelFormat.Rgba, PixelType.UnsignedByte, frame.Data);

            var frameData = (Span<byte>) frame.Data;
            
            // Flip the image vertically
            for (var y = 0; y < size.Y / 2; y++)
            {
                var topRow = y * size.X * 4;
                var bottomRow = (size.Y - y - 1) * size.X * 4;
                
                // Swap the rows
                var temp = new byte[size.X * 4];
                frameData.Slice(topRow, size.X * 4).CopyTo(temp);
                frameData.Slice(bottomRow, size.X * 4).CopyTo(frameData.Slice(topRow, size.X * 4));
                temp.CopyTo(frameData.Slice(bottomRow, size.X * 4));
            }
            
            CurrentFrame = frame;
            
            GLFW.SwapBuffers(window);
        }
        
        GLFW.MakeContextCurrent(null);
    }

    public void Close()
    {
        GLFW.SetWindowShouldClose(window, true);
    }
    
    public void Dispose()
    {
        GLFW.DestroyWindow(window);
    }
}