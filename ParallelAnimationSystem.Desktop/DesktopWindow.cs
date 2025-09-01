using Ico.Reader;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ParallelAnimationSystem.Windowing;
using ReFuel.Stb;

namespace ParallelAnimationSystem.Desktop;

public unsafe class DesktopWindow : IWindow, IDisposable
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

    public DesktopWindow(string title, Vector2i size, GLContextSettings glContextSettings, DesktopWindowSettings windowSettings)
    {
        if (glContextSettings.ES)
        {
            GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlEsApi);
        }
        else
        {
            GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlApi);
            GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);
        }
            
        GLFW.WindowHint(WindowHintInt.ContextVersionMajor, glContextSettings.Version.Major);
        GLFW.WindowHint(WindowHintInt.ContextVersionMinor, glContextSettings.Version.Minor);
        
        GLFW.WindowHint(WindowHintBool.TransparentFramebuffer, windowSettings.Transparent);
        GLFW.WindowHint(WindowHintBool.Decorated, !windowSettings.Borderless);
        GLFW.WindowHint(WindowHintBool.Resizable, windowSettings.Resizable);
        GLFW.WindowHint(WindowHintBool.Floating, windowSettings.Floating);
        
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
        GLFW.MakeContextCurrent(window);
        GLFW.SwapInterval(interval);
    }

    public void RequestAnimationFrame(AnimationFrameCallback callback)
    {
        GLFW.PollEvents();
        
        GLFW.MakeContextCurrent(window);
        var time = GLFW.GetTime();
        if (callback(time, 0))
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
}