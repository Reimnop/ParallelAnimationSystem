using System;
using Avalonia.VisualTree;
using OpenTK.Graphics.OpenGLES2;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Platform.OpenGL;

namespace ParallelAnimationSystem.Avalonia.Integration;

public class PASSurface(PASView view) : IOpenGLSurface, IDisposable
{
    public Vector2i FramebufferSize
    {
        get
        {
            var size = view.Bounds.Size;
            var scaling = view.GetPresentationSource()?.RenderScaling ?? 1.0;
            return new Vector2i(
                (int)(size.Width * scaling), 
                (int)(size.Height * scaling));
        }
    }

    // Avalonia does not lose the context
    public bool IsContextLost => false;

    public int TargetFramebufferHandle { get; set; }

    private readonly int framebuffer = GL.GenFramebuffer();

    public void MakeContextCurrent()
    {
    }

    public void Present(int texture, Vector2i size, ColorRgba clearColor)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, texture, 0);
        
        var dstSize = FramebufferSize;
        
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebuffer);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, TargetFramebufferHandle);
        
        // Clear the default framebuffer
        GL.ClearColor(clearColor.R, clearColor.G, clearColor.B, clearColor.A);
        GL.Viewport(0, 0, dstSize.X, dstSize.Y);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        // Blit the framebuffer to the default framebuffer
        GL.BlitFramebuffer(
            0, 0, size.X, size.Y,
            0, 0, dstSize.X, dstSize.Y,
            ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
    }

    public void Dispose()
    {
        GL.DeleteFramebuffer(framebuffer);
    }
}