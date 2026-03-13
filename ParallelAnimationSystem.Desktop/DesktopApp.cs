using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Desktop;

public sealed class DesktopApp(IServiceProvider serviceProvider)
{
    private volatile bool appRunning = true;
    
    public void StartApp(string beatmapPath, string audioPath, float startTime = 0.0f)
    {
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        
        // Load beatmap
        BeatmapHelper.ReadBeatmap(beatmapPath, out var beatmapData, out var beatmapFormat);
        var beatmapService = sp.GetRequiredService<BeatmapService>();
        beatmapService.LoadBeatmap(beatmapData, beatmapFormat);
        
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
            appDirector.ProcessFrame((float) audioPlayer.Position);
        
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
        var window = sp.GetRequiredService<IWindow>();
        
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
}