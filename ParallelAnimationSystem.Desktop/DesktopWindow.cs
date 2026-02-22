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
    private const string Title = "Parallel Animation System";

    // Called every time events are polled
    public event EventHandler? EventsPolled; 
    
    public Vector2i FramebufferSize
    {
        get
        {
            GLFW.GetFramebufferSize(window, out var width, out var height);
            return new Vector2i(width, height);
        }
    }
    
    public bool ShouldClose => GLFW.WindowShouldClose(window);
    
    public bool IsContextCurrent => GLFW.GetCurrentContext() == window;
    
    public Window* Handle => window;

    private readonly Window* window;

    public DesktopWindow(DesktopWindowSettings windowSettings, OpenGLSettings glSettings)
    {
        // Initialize GLFW
        if (!GLFW.Init())
            throw new Exception("Failed to initialize GLFW");
        
        if (glSettings.IsES)
        {
            GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlEsApi);
        }
        else
        {
            GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlApi);
            GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);
        }

        if (windowSettings.UseEgl) 
        {
            GLFW.WindowHint(WindowHintContextApi.ContextCreationApi, ContextApi.EglContextApi);
        }
        
        GLFW.WindowHint(WindowHintInt.ContextVersionMajor, glSettings.MajorVersion);
        GLFW.WindowHint(WindowHintInt.ContextVersionMinor, glSettings.MinorVersion);
        
        window = GLFW.CreateWindow(windowSettings.Size.X, windowSettings.Size.Y, Title, null, null);
        
        GLFW.MakeContextCurrent(window);
        GLFW.SwapInterval(windowSettings.VSync ? 1 : 0);
        
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

    public void PollEvents()
    {
        GLFW.PollEvents();
        
        EventsPolled?.Invoke(this, EventArgs.Empty);
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
        
        GLFW.SwapBuffers(window);
    }

    public IntPtr GetProcAddress(string procName)
        => GLFW.GetProcAddress(procName);

    public void Close()
    {
        GLFW.SetWindowShouldClose(window, true);
    }
    
    public void Dispose()
    {
        GLFW.DestroyWindow(window);
        GLFW.Terminate();
    }
    
    [DllImport("Dwmapi.dll")]
    static extern int DwmFlush();
}