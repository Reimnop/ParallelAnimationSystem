using System.Numerics;
using OpenTK.Graphics.OpenGL;
using ParallelAnimationSystem.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ParallelAnimationSystem.Windowing;
using ParallelAnimationSystem.Windowing.OpenGL;

namespace ParallelAnimationSystem.FFmpeg;

public unsafe class FFmpegWindow : IOpenGLWindow, IDisposable
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

    public FFmpegWindow(string title, FFmpegWindowSettings settings, OpenGLSettings glSettings)
    {
        if (glSettings.IsES)
        {
            GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlEsApi);
        }
        else
        {
            GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlApi);
            GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);
        }

        if (settings.UseEgl) 
        {
            GLFW.WindowHint(WindowHintContextApi.ContextCreationApi, ContextApi.EglContextApi);
        }
        
        GLFW.WindowHint(WindowHintInt.ContextVersionMajor, glSettings.MajorVersion);
        GLFW.WindowHint(WindowHintInt.ContextVersionMinor, glSettings.MinorVersion);
        GLFW.WindowHint(WindowHintBool.Visible, false);
        GLFW.WindowHint(WindowHintBool.Resizable, false);
        GLFW.WindowHint(WindowHintBool.Decorated, false);
        
        window = GLFW.CreateWindow(settings.Size.X, settings.Size.Y, title, null, null);
    }
    
    public void MakeContextCurrent()
    {
        GLFW.MakeContextCurrent(window);
    }

    public void PollEvents()
    {
        GLFW.PollEvents();
    }

    public void SetSwapInterval(int interval)
    {
        GLFW.MakeContextCurrent(window);
        GLFW.SwapInterval(interval);
    }

    public void Present(int framebuffer, Vector4 clearColor, Vector2i size, Vector2i offset)
    {
        MakeContextCurrent();
        
        var dstSize = FramebufferSize;
        
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebuffer);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        
        // Clear the default framebuffer
        GL.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
        GL.Viewport(0, 0, dstSize.X, dstSize.Y);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        // Blit the framebuffer to the default framebuffer
        GL.BlitFramebuffer(
            0, 0, size.X, size.Y,
            offset.X, offset.Y, offset.X + size.X, offset.Y + size.Y,
            ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
        
        // Read pixels from the default framebuffer
        var frame = new FrameData(dstSize.X, dstSize.Y);
            
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.ReadPixels(0, 0, dstSize.X, dstSize.Y, PixelFormat.Rgba, PixelType.UnsignedByte, frame.Data);

        var frameData = (Span<byte>) frame.Data;
            
        // Flip the image vertically
        for (var y = 0; y < dstSize.Y / 2; y++)
        {
            var topRow = y * dstSize.X * 4;
            var bottomRow = (dstSize.Y - y - 1) * dstSize.X * 4;
                
            // Swap the rows
            var temp = new byte[dstSize.X * 4];
            frameData.Slice(topRow, dstSize.X * 4).CopyTo(temp);
            frameData.Slice(bottomRow, dstSize.X * 4).CopyTo(frameData.Slice(topRow, dstSize.X * 4));
            temp.CopyTo(frameData.Slice(bottomRow, dstSize.X * 4));
        }
            
        CurrentFrame = frame;
            
        // GLFW.SwapBuffers(window);
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