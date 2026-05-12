using Microsoft.Extensions.DependencyInjection;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Desktop;

public sealed class DesktopApp(IServiceProvider serviceProvider)
{
    private enum SeekAction
    {
        None,
        Forward10,
        Backward10,
        Forward5,
        Backward5
    }
    
    private struct FullscreenData
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;
    }
    
    private volatile bool appRunning = true;
    
    private SeekAction seekAction;
    private FullscreenData? fullscreenData;
    
    public void StartApp(string beatmapPath, string audioPath, float startTime = 0.0f)
    {
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        
        // Load beatmap
        // BeatmapHelper.ReadBeatmap(beatmapPath, out var beatmapData, out var beatmapFormat);
        var beatmapService = sp.GetRequiredService<BeatmapService>();
        // beatmapService.LoadBeatmap(beatmapData, beatmapFormat);
        beatmapService.LoadBeatmap(beatmapPath);
        
        // Initialize core service
        var appDirector = sp.GetRequiredService<AppDirector>();
        
        // Play audio
        using var audioPlayer = AudioPlayer.Load(audioPath);
        audioPlayer.Position = startTime;
        audioPlayer.Play();
        
        // Start render thread
        var renderThread = new Thread(StartRenderThread);
        renderThread.Start();
        
        // Start the main loop
        while (appRunning)
        {
            if (seekAction != SeekAction.None)
            {
                switch (seekAction)
                {
                    case SeekAction.Forward10:
                        audioPlayer.Position += 10.0f;
                        break;
                    case SeekAction.Backward10:
                        audioPlayer.Position -= 10.0f;
                        break;
                    case SeekAction.Forward5:
                        audioPlayer.Position += 5.0f;
                        break;
                    case SeekAction.Backward5:
                        audioPlayer.Position -= 5.0f;
                        break;
                }

                seekAction = SeekAction.None;
            }
            
            appDirector.ProcessFrame((float) audioPlayer.Position);
        }
        
        // Stop audio
        audioPlayer.Stop();
        
        // Wait for the render thread to finish
        renderThread.Join();
    }

    private void StartRenderThread()
    {
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        
        var renderQueue = (AsyncRenderQueue) sp.GetRequiredService<IRenderQueue>();
        var renderer = sp.GetRequiredService<IRenderer>();
        var window = (DesktopWindow) sp.GetRequiredService<IWindow>();

        unsafe
        {
            var windowPtr = window.Handle;
            GLFW.SetKeyCallback(windowPtr, OnKey);
        }
        
        // Start the render loop
        while (!window.ShouldClose)
        {
            window.PollEvents();

            while (renderQueue.QueuedFrames == 0)
                Thread.Yield();
            
            renderQueue.FlushOneFrame(renderer);
        }
        
        // Signal the main thread to stop
        appRunning = false;
    }

    private unsafe void OnKey(Window* window, Keys key, int scanCode, InputAction action, KeyModifiers mods)
    {
        if (action == InputAction.Press)
        {
            switch (key)
            {
                case Keys.Escape:
                    GLFW.SetWindowShouldClose(window, true);
                    break;
                case Keys.F11:
                {
                    if (fullscreenData == null)
                    {
                        var monitor = GLFW.GetPrimaryMonitor();
                        var mode = GLFW.GetVideoMode(monitor);
                        GLFW.GetWindowPos(window, out var x, out var y);
                        GLFW.GetWindowSize(window, out var width, out var height);
                        fullscreenData = new FullscreenData
                        {
                            X = x,
                            Y = y,
                            Width = width,
                            Height = height
                        };
                        GLFW.SetWindowMonitor(window, monitor, 0, 0, mode->Width, mode->Height, mode->RefreshRate);
                    }
                    else
                    {
                        GLFW.SetWindowMonitor(
                            window, 
                            null, 
                            fullscreenData.Value.X, 
                            fullscreenData.Value.Y,
                            fullscreenData.Value.Width, 
                            fullscreenData.Value.Height, 
                            0);
                        fullscreenData = null;
                    }
                    break;
                }
                case Keys.J:
                    seekAction = SeekAction.Backward10;
                    break;
                case Keys.L:
                    seekAction = SeekAction.Forward10;
                    break;
                case Keys.Left:
                    seekAction = SeekAction.Backward5;
                    break;
                case Keys.Right:
                    seekAction = SeekAction.Forward5;
                    break;
            }
        }
    }
}