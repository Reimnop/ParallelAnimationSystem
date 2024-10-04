using System.Runtime.CompilerServices;
using OpenTK.Mathematics;
using ParallelAnimationSystem.Windowing;
using SDL;
using static SDL.SDL3;

namespace ParallelAnimationSystem.Android;

public unsafe class AndroidWindow : IWindow, IDisposable
{
    public string Title
    {
        get => SDL_GetWindowTitle(window) ?? string.Empty;
        set => SDL_SetWindowTitle(window, value);
    }

    public Vector2i FramebufferSize
    {
        get
        {
            int width, height;
            SDL_GetWindowSizeInPixels(window, &width, &height);
            return new Vector2i(width, height);
        }
        set => SDL_SetWindowSize(window, value.X, value.Y);
    }
    
    public bool ShouldClose { get; private set; }
    
    private const int EventsPerPeep = 64;
    private readonly SDL_Event[] events = new SDL_Event[EventsPerPeep];
    
    private readonly SDL_Window* window;
    private readonly SDL_GLContextState* context;
    
    public AndroidWindow(string title, Vector2i size, GLContextSettings glContextSettings)
    {
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_RED_SIZE, 8);
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_GREEN_SIZE, 8);
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_BLUE_SIZE, 8);
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_ALPHA_SIZE, 0);
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, glContextSettings.Version.Major);
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, glContextSettings.Version.Minor);
        SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_ES);
        
        window = SDL_CreateWindow(
            title, 
            size.X, size.Y, 
            SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY | SDL_WindowFlags.SDL_WINDOW_OPENGL);
        if (window == null)
            throw new Exception("Failed to create window");
        
        // This hides the navigation bar on Android
        SDL_SetWindowFullscreen(window, SDL_bool.SDL_TRUE);
        
        context = SDL_GL_CreateContext(window);
        if (context == null)
            throw new Exception("Failed to create OpenGL context");
    }

    public void MakeContextCurrent()
    {
        WaitUntilSurfaceReady();
        
        SDL_GL_MakeCurrent(window, context);
    }

    public void SetSwapInterval(int interval)
    {
        WaitUntilSurfaceReady();
        
        SDL_GL_MakeCurrent(window, context);
        SDL_GL_SetSwapInterval(interval);
    }

    public void RequestAnimationFrame(AnimationFrameCallback callback)
    {
        WaitUntilSurfaceReady();
        
        SDL_GL_MakeCurrent(window, context);
        
        PollEvents();
        var time = SDL_GetTicks() / 1000.0f;
        if (callback(time))
            SDL_GL_SwapWindow(window);
    }

    public void Close()
    {
        ShouldClose = true;
    }
    
    public void Dispose()
    {
        SDL_GL_DestroyContext(context);
        SDL_DestroyWindow(window);
    }
    
    private void PollEvents()
    {
        SDL_PumpEvents();

        int eventsRead;
        do
        {
            eventsRead = SDL_PeepEvents(events, SDL_EventAction.SDL_GETEVENT, SDL_EventType.SDL_EVENT_FIRST, SDL_EventType.SDL_EVENT_LAST);
            for (var i = 0; i < eventsRead; i++)
            {
                var ev = events[i];
                switch (ev.type)
                {
                    case (uint) SDL_EventType.SDL_EVENT_QUIT:
                        ShouldClose = true;
                        break;
                }
            }
        } while (eventsRead == EventsPerPeep);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WaitUntilSurfaceReady()
    {
        while (!PasActivity.SurfaceReady)
            Thread.Yield();
    }
}