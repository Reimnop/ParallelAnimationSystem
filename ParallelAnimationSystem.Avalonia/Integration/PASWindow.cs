using System;
using System.Numerics;
using Avalonia.OpenGL;
using Avalonia.VisualTree;
using OpenTK.Graphics.OpenGLES2;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Windowing.OpenGL;

namespace ParallelAnimationSystem.Avalonia.Integration;

public class PASWindow(PASView view, GlInterface gl) : IOpenGLWindow
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
    public bool ShouldClose => false;

    public bool IsContextCurrent => true;

    public int TargetFramebufferHandle { get; set; }

    public void MakeContextCurrent()
    {
    }

    public void Present(int framebuffer, Vector4 clearColor, Vector2i size, Vector2i offset)
    {
        var dstSize = FramebufferSize;
        
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebuffer);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, TargetFramebufferHandle);
        
        // Clear the default framebuffer
        GL.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
        GL.Viewport(0, 0, dstSize.X, dstSize.Y);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        // Blit the framebuffer to the default framebuffer
        GL.BlitFramebuffer(
            0, 0, size.X, size.Y,
            offset.X, offset.Y, offset.X + size.X, offset.Y + size.Y,
            ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
    }

    public IntPtr GetProcAddress(string procName)
        => gl.GetProcAddress(procName);
    
    public void PollEvents()
    {
    }

    public void Close()
    {
    }
}