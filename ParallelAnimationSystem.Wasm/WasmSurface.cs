using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.Egl;
using OpenTK.Graphics.OpenGLES2;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Platform.OpenGL;

namespace ParallelAnimationSystem.Wasm;

public class WasmSurface : IOpenGLSurface, IDisposable
{
    private class BindingsContext : IBindingsContext
    {
        public IntPtr GetProcAddress(string procName)
            => Egl.GetProcAddress(procName);
    }
    
    public Vector2i RenderSize
    {
        get
        {
            Egl.QuerySurface(display, surface, Egl.WIDTH, out var width);
            Egl.QuerySurface(display, surface, Egl.HEIGHT, out var height);
            return new Vector2i(width, height);
        }
    }

    public bool IsContextLost { get; private set; }

    private readonly IntPtr display;
    private readonly IntPtr context;
    private readonly IntPtr surface;
    
    private readonly int framebuffer;
    
    public WasmSurface(OpenGLSettings glSettings)
    {
        if (!glSettings.IsES)
            throw new InvalidOperationException("Only OpenGL ES is supported on WebAssembly");
        
        display = Egl.GetDisplay(IntPtr.Zero);
        if (display == IntPtr.Zero)
            throw new InvalidOperationException("Failed to get EGL display");
        
        if (!Egl.Initialize(display, out _, out _))
            throw new InvalidOperationException("Failed to initialize EGL");
        
        var config = new[]
        {
            Egl.RED_SIZE, 8,
            Egl.GREEN_SIZE, 8,
            Egl.BLUE_SIZE, 8,
            Egl.ALPHA_SIZE, 0,
            Egl.DEPTH_SIZE, 0,
            Egl.STENCIL_SIZE, 0,
            Egl.SURFACE_TYPE, Egl.WINDOW_BIT,
            Egl.RENDERABLE_TYPE, glSettings.MajorVersion == 3 ? Egl.OPENGL_ES3_BIT : Egl.OPENGL_ES2_BIT,
            Egl.SAMPLES, 0,
            Egl.NONE
        };

        var configs = new IntPtr[1];
        if (!Egl.ChooseConfig(display, config, configs, 1, out var numConfig))
            throw new InvalidOperationException("Failed to choose EGL config");
        
        if (numConfig == 0)
            throw new InvalidOperationException("No EGL configs found");
        
        var chosenConfig = configs[0];
        
        if (!Egl.BindAPI(RenderApi.ES))
            throw new InvalidOperationException("Failed to bind ES API");
        
        var ctxAttribs = new[]
        {
            Egl.CONTEXT_CLIENT_VERSION, glSettings.MajorVersion,
            Egl.NONE
        };

        context = Egl.CreateContext(display, chosenConfig, IntPtr.Zero, ctxAttribs);
        if (context == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create EGL context");
        
        surface = Egl.CreateWindowSurface(display, chosenConfig, IntPtr.Zero, IntPtr.Zero);
        if (surface == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create EGL surface");
        
        if (!Egl.MakeCurrent(display, surface, surface, context))
            throw new InvalidOperationException("Failed to make context current");
        
        GLLoader.LoadBindings(new BindingsContext());
        
        framebuffer = GL.GenFramebuffer();
    }

    public void Present(int texture, Vector2i size, ColorRgba clearColor)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, texture, 0);
        
        var dstSize = RenderSize;
        
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebuffer);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        
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
        Egl.MakeCurrent(display, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        Egl.DestroyContext(display, context);
        Egl.DestroySurface(display, surface);
        Egl.Terminate(display);
        
        IsContextLost = true;
    }
}