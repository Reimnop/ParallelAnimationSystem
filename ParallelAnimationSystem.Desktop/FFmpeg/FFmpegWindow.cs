using OpenTK.Graphics.OpenGL;
using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Desktop.FFmpeg;

public class FFmpegWindow(DesktopWindowSettings windowSettings, GlfwService glfw) : DesktopWindow(windowSettings, glfw)
{
    public ReadOnlySpan<byte> FrameData => frameData;
    
    private readonly byte[] frameData = new byte[windowSettings.Size.X * windowSettings.Size.Y * 4]; // RGBA8

    protected override void OnFramePresent(Vector2i framebufferSize)
    {
        base.OnFramePresent(framebufferSize);
        
        // Read pixels from the framebuffer into the frameData array
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
        GL.ReadPixels(
            0, 0,
            framebufferSize.X, framebufferSize.Y,
            PixelFormat.Rgba, PixelType.UnsignedByte,
            frameData);
    }
}