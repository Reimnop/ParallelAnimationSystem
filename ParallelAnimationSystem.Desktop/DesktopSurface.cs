using Ico.Reader;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Platform.OpenGL;
using ParallelAnimationSystem.Rendering;
using ReFuel.Stb;

namespace ParallelAnimationSystem.Desktop;

public unsafe class DesktopSurface : IOpenGLSurface, IDisposable
{
    private const string Title = "Parallel Animation System";
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
            GLFW.GetFramebufferSize(window, out var width, out var height);
            return new Vector2i(width, height);
        }
    }

    public bool IsContextLost { get; set; }
    public bool ShouldClose => GLFW.WindowShouldClose(window);
    
    public Window* WindowPtr => window;
    
    private readonly DesktopSurfaceSettings surfaceSettings;
    private readonly GlfwService glfw;
    private readonly Window* window;

    private readonly int framebuffer;

    public DesktopSurface(DesktopSurfaceSettings surfaceSettings, GlfwService glfw)
    {
        this.surfaceSettings = surfaceSettings;
        this.glfw = glfw;
        
        window = this.glfw.CreateWindowHandle(this.surfaceSettings.Size.X, this.surfaceSettings.Size.Y, Title, this.surfaceSettings.UseEgl);
        
        GLFW.MakeContextCurrent(window);
        GLFW.SwapInterval(surfaceSettings.VSync ? 1 : 0);

        framebuffer = GL.GenFramebuffer();
        
        // Load window icon
        using var iconStream = typeof(DesktopSurface).Assembly.GetManifestResourceStream("ParallelAnimationSystem.Desktop.icon.ico");
        if (iconStream is not null)
            LoadIcon(iconStream);
    }
    
    public void Dispose()
    {
        // We don't need to dispose the framebuffer because
        // it will be deleted automatically when context is lost
        
        GLFW.DestroyWindow(window);
        IsContextLost = true;
    }

    private void LoadIcon(Stream iconStream)
    {
        var icoReader = new IcoReader();
        var iconData = icoReader.Read(iconStream);
        if (iconData is null)
            return;
        var group = iconData.Groups[0];
        var images = new List<StbImage>();
        try
        {
            for (var i = 0; i < group.DirectoryEntries.Length; i++)
            {
                var data = iconData.GetImage(group.Name, i);
                var image = StbImage.Load(data);
                images.Add(image);
            }
            var glfwImages = images
                .Select(image => new Image
                {
                    Width = image.Width,
                    Height = image.Height,
                    Pixels = (byte*) image.ImagePointer,
                });
            GLFW.SetWindowIcon(window, glfwImages.ToArray());
        }
        finally
        {
            foreach (var image in images)
                image.Dispose();
        }
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
        
        GLFW.SwapBuffers(window);
        
        OnFramePresent(framebufferSize);
    }

    public void PollEvents()
    {
        glfw.PollEvents();
    }

    public IntPtr GetProcAddress(string procName)
        => GLFW.GetProcAddress(procName);

    protected virtual void OnFramePresent(Vector2i framebufferSize)
    {
    }
}