using Ico.Reader;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Platform.OpenGL;
using ReFuel.Stb;

namespace ParallelAnimationSystem.Desktop;

public unsafe class DesktopSurface : IOpenGLSurface, IDisposable
{
    private const string Title = "Parallel Animation System";
    
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

    private readonly GlfwService glfw;
    private readonly Window* window;

    private readonly int framebuffer;

    public DesktopSurface(DesktopWindowSettings windowSettings, GlfwService glfw)
    {
        this.glfw = glfw;
        
        window = this.glfw.CreateWindowHandle(windowSettings.Size.X, windowSettings.Size.Y, Title, windowSettings.UseEgl);
        
        GLFW.MakeContextCurrent(window);
        GLFW.SwapInterval(windowSettings.VSync ? 1 : 0);

        framebuffer = GL.GenFramebuffer();
        
        // Load window icon
        using var iconStream = typeof(DesktopSurface).Assembly.GetManifestResourceStream("ParallelAnimationSystem.Desktop.icon.ico");
        if (iconStream is not null)
            LoadIcon(iconStream);
    }
    
    public void Dispose()
    {
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

    public void MakeContextCurrent()
    {
        GLFW.MakeContextCurrent(window);
    }

    public void Present(int texture, Vector2i size, ColorRgba clearColor)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, texture, 0);
        
        var dstSize = FramebufferSize;
        
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebuffer);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        
        // Clear the default framebuffer
        GL.ClearColor(clearColor.R, clearColor.G, clearColor.B, clearColor.A);
        GL.Viewport(0, 0, size.X, size.Y);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        // Blit the framebuffer to the default framebuffer
        GL.BlitFramebuffer(
            0, 0, size.X, size.Y,
            0, 0, dstSize.X, dstSize.Y,
            ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
        
        GLFW.SwapBuffers(window);
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