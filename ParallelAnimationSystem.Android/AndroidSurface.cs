using System.Diagnostics;
using System.Runtime.InteropServices;
using Android.Runtime;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.Egl;
using OpenTK.Graphics.OpenGLES2;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Platform.OpenGL;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem.Android;

public class AndroidSurface : IOpenGLSurface, IDisposable
{
    private class BindingsContext : IBindingsContext
    {
        public IntPtr GetProcAddress(string procName)
            => Egl.GetProcAddress(procName);
    }
    
    private const string LibAndroid = "libandroid.so";
    private const float LockedAspectRatio = 16f / 9f;
    
    public Vector2i RenderSize
    {
        get
        {
            var size = FramebufferSize;
            RenderUtil.GetRenderSize(size, surfaceSettings.LockAspectRatio ? LockedAspectRatio : null, out var renderSize);
            return renderSize;
        }
    }

    public Vector2i FramebufferSize
    {
        get
        {
            if (!Egl.QuerySurface(eglDisplay, eglSurface, Egl.WIDTH, out var width))
                throw new Exception("Failed to query surface width");
            
            if (!Egl.QuerySurface(eglDisplay, eglSurface, Egl.HEIGHT, out var height))
                throw new Exception("Failed to query surface height");
            
            return new Vector2i(width, height);
        }
    }
    
    public bool ShouldClose { get; private set; }
    public bool IsContextLost => Egl.GetCurrentContext() == eglContext;

    private readonly AndroidSurfaceSettings surfaceSettings;

    private readonly IntPtr aNativeWindowPtr;
    private readonly IntPtr eglDisplay;
    private readonly IntPtr eglSurface;
    private readonly IntPtr eglContext;

    private readonly int framebuffer;

    public AndroidSurface(OpenGLSettings glSettings, AndroidSurfaceSettings surfaceSettings, AndroidSurfaceContext surfaceContext)
    {
        this.surfaceSettings = surfaceSettings;
        
        if (!glSettings.IsES)
            throw new NotSupportedException("Desktop OpenGL is not supported on Android, use OpenGL ES instead");
        
        // Initialize EGL
        eglDisplay = Egl.GetDisplay(Egl.DEFAULT_DISPLAY);
        
        Egl.Initialize(eglDisplay, out _, out _);
        int[] configAttribs = 
        [
            Egl.RENDERABLE_TYPE, glSettings.MajorVersion == 3 ? Egl.OPENGL_ES3_BIT : Egl.OPENGL_ES2_BIT,
            Egl.SURFACE_TYPE,    Egl.WINDOW_BIT,
            Egl.RED_SIZE,        8,
            Egl.GREEN_SIZE,      8,
            Egl.BLUE_SIZE,       8,
            Egl.ALPHA_SIZE,      0,
            Egl.NONE
        ];
        
        var configs = new IntPtr[1];
        Egl.ChooseConfig(eglDisplay, configAttribs, configs, 1, out _);
        
        var config = configs[0];
        if (config == IntPtr.Zero)
            throw new Exception("Failed to choose EGL config");
        
        int[] contextAttribs = 
        [
            Egl.CONTEXT_CLIENT_VERSION, glSettings.MajorVersion,
            Egl.NONE
        ];
        
        eglContext = Egl.CreateContext(eglDisplay, config, IntPtr.Zero, contextAttribs);
        if (eglContext == IntPtr.Zero)
            throw new Exception("Failed to create EGL context");
        
        var surface = surfaceContext.SurfaceHolder.Surface;
        Debug.Assert(surface is not null);
        
        aNativeWindowPtr = ANativeWindow_fromSurface(JNIEnv.Handle, surface.Handle);
        if (aNativeWindowPtr == IntPtr.Zero)
            throw new Exception("Failed to get ANativeWindow pointer from surface");
        
        eglSurface = Egl.CreateWindowSurface(eglDisplay, config, aNativeWindowPtr, IntPtr.Zero);
        if (eglSurface == IntPtr.Zero)
            throw new Exception("Failed to create EGL window surface");
        
        GLLoader.LoadBindings(new BindingsContext());
        
        framebuffer = GL.GenFramebuffer();
    }

    public void Present(int texture, Vector2i size, ColorRgba clearColor)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, texture, 0);
        
        var framebufferSize = FramebufferSize;
        var offset = (framebufferSize - size) / 2;
        
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebuffer);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        
        // Clear the default framebuffer
        GL.ClearColor(clearColor.R, clearColor.G, clearColor.B, clearColor.A);
        GL.Viewport(0, 0, framebufferSize.X, framebufferSize.Y);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        // Blit the framebuffer to the default framebuffer
        GL.BlitFramebuffer(
            0, 0, size.X, size.Y,
            offset.X, offset.Y, offset.X + size.X, offset.Y + size.Y,
            ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
        
        Egl.SwapBuffers(eglDisplay, eglSurface);
    }

    public void Close()
    {
        ShouldClose = true;
    }
    
    public void Dispose()
    {
        // We don't need to dispose the framebuffer because
        // it will be deleted automatically when context is lost
        
        Egl.MakeCurrent(eglDisplay, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        Egl.DestroySurface(eglDisplay, eglSurface);
        Egl.DestroyContext(eglDisplay, eglContext);
        Egl.Terminate(eglDisplay);
        
        ANativeWindow_release(aNativeWindowPtr);
    }
    
    [DllImport(LibAndroid)]
    private static extern IntPtr ANativeWindow_fromSurface(IntPtr env, IntPtr surface);
    
    [DllImport(LibAndroid)]
    public static extern void ANativeWindow_release(IntPtr window);
}