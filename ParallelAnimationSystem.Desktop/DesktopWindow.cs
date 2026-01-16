using System.Numerics;
using System.Runtime.InteropServices;
using Ico.Reader;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Windowing.OpenGL;
using ReFuel.Stb;

namespace ParallelAnimationSystem.Desktop;

public unsafe class DesktopWindow : IOpenGLWindow, IDisposable
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

    private readonly Window* window;

    public DesktopWindow(string title, Vector2i size, OpenGLSettings glSettings)
    {
        GLFW.WindowHint(WindowHintClientApi.ClientApi, glSettings.IsES ? ClientApi.OpenGlEsApi : ClientApi.OpenGlApi);
        if (!glSettings.IsES)
            GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);
        GLFW.WindowHint(WindowHintInt.ContextVersionMajor, glSettings.MajorVersion);
        GLFW.WindowHint(WindowHintInt.ContextVersionMinor, glSettings.MinorVersion);
        
        window = GLFW.CreateWindow(size.X, size.Y, title, null, null);
        
        // Load window icon
        using var iconStream = typeof(DesktopWindow).Assembly.GetManifestResourceStream("ParallelAnimationSystem.Desktop.icon.ico");
        if (iconStream is not null)
            LoadIcon(iconStream);
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

    public void SetSwapInterval(int interval)
    {
        MakeContextCurrent();
        GLFW.SwapInterval(interval);
    }

    public void PollEvents()
    {
        GLFW.PollEvents();
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
            offset.X, offset.Y, size.X, size.Y,
            0, 0, dstSize.X, dstSize.Y,
            ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
        
        GLFW.SwapBuffers(window);
    }

    public void Close()
    {
        GLFW.SetWindowShouldClose(window, true);
    }
    
    public void Dispose()
    {
        GLFW.DestroyWindow(window);
    }
    
    [DllImport("Dwmapi.dll")]
    static extern int DwmFlush();
}