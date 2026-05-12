using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Util;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Desktop;

public sealed class DesktopApp(IServiceProvider serviceProvider)
{
    private enum ButtonAction
    {
        None,
        Forward10,
        Backward10,
        Forward5,
        Backward5,
        PlayPause,
        RandomizeSeed
    }
    
    private struct FullscreenData
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;
    }
    
    private volatile bool appRunning = true;
    
    private ButtonAction buttonAction;
    private FullscreenData? fullscreenData;
    
    public void StartApp(string beatmapPath, string audioPath, float startTime, ulong? randomSeed)
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
        
        // Get the random seed service
        var rss = sp.GetRequiredService<RandomSeedService>();
        rss.Seed = randomSeed ?? NumberUtil.SplitMix64((ulong)DateTimeOffset.Now.ToUnixTimeSeconds());
        
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
            if (buttonAction != ButtonAction.None)
            {
                switch (buttonAction)
                {
                    case ButtonAction.Forward10:
                        audioPlayer.Position += 10.0f;
                        break;
                    case ButtonAction.Backward10:
                        audioPlayer.Position -= 10.0f;
                        break;
                    case ButtonAction.Forward5:
                        audioPlayer.Position += 5.0f;
                        break;
                    case ButtonAction.Backward5:
                        audioPlayer.Position -= 5.0f;
                        break;
                    case ButtonAction.PlayPause:
                        if (audioPlayer.Playing)                            
                            audioPlayer.Pause();
                        else                            
                            audioPlayer.Play();
                        break;
                    case ButtonAction.RandomizeSeed:
                        rss.Seed = NumberUtil.SplitMix64((ulong)DateTimeOffset.Now.ToUnixTimeSeconds());
                        break;
                }

                buttonAction = ButtonAction.None;
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
        
        GCHandle keyCallbackHandle;
        
        unsafe
        {
            GLFWCallbacks.KeyCallback keyCallback = OnKey;
            keyCallbackHandle = GCHandle.Alloc(keyCallback);
            
            var windowPtr = window.Handle;
            GLFW.SetKeyCallback(windowPtr, keyCallback);
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
        
        // Free the GCHandle for the key callback
        keyCallbackHandle.Free();
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
                    buttonAction = ButtonAction.Backward10;
                    break;
                case Keys.L:
                    buttonAction = ButtonAction.Forward10;
                    break;
                case Keys.Left:
                    buttonAction = ButtonAction.Backward5;
                    break;
                case Keys.Right:
                    buttonAction = ButtonAction.Forward5;
                    break;
                case Keys.Space:
                    buttonAction = ButtonAction.PlayPause;
                    break;
                case Keys.R:
                    buttonAction = ButtonAction.RandomizeSeed;
                    break;
            }
        }
    }
}