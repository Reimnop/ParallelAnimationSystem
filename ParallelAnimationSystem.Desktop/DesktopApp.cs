using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Desktop;

public sealed class DesktopApp
{
    private readonly ConcurrentQueue<IDrawList> drawListPool = [];
    private readonly ConcurrentQueue<IDrawList> queuedDrawLists = [];

    private readonly IServiceProvider serviceProvider;

    private volatile bool running = true;

    public DesktopApp(IServiceProvider serviceProvider, IRenderingFactory renderingFactory)
    {
        this.serviceProvider = serviceProvider;

        for (var i = 0; i < 3; i++)
            drawListPool.Enqueue(renderingFactory.CreateDrawList());
    }

    public void StartApp(string beatmapPath, string audioPath)
    {
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        
        // Load beatmap
        BeatmapHelper.ReadBeatmap(beatmapPath, out var beatmapData, out var beatmapFormat);
        var beatmapService = sp.GetRequiredService<BeatmapService>();
        beatmapService.LoadBeatmap(beatmapData, beatmapFormat);
        
        // Initialize core service
        var appCore = sp.GetRequiredService<AppCore>();
        
        // Play audio
        using var audioPlayer = AudioPlayer.Load(audioPath);
        audioPlayer.Play();
        
        // Start render thread
        var renderThread = new Thread(StartRenderThread);
        renderThread.Start();
        
        // Start the main thread
        while (running)
        {
            // Get a draw list from the pool
            IDrawList? drawList;
            while (!drawListPool.TryDequeue(out drawList))
                Thread.Yield();
            
            // Populate the draw list
            appCore.ProcessFrame((float) audioPlayer.Position, drawList);
            
            // Enqueue the draw list for rendering
            queuedDrawLists.Enqueue(drawList);
        }
        
        // Wait for render thread to finish
        renderThread.Join();
        
        // Stop audio
        audioPlayer.Stop();
    }

    private void StartRenderThread()
    {
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        
        // Initialize renderer
        var renderer = sp.GetRequiredService<IRenderer>();
        var window = sp.GetRequiredService<IWindow>();

        while (!window.ShouldClose)
        {
            window.PollEvents();

            // Get a draw list from the queue
            IDrawList? drawList;
            while (!queuedDrawLists.TryDequeue(out drawList))
                Thread.Yield();
            
            // Process the frame
            renderer.ProcessFrame(drawList);
            
            // Return the draw list to the pool
            drawList.Clear();
            drawListPool.Enqueue(drawList);
        }

        running = false;
    }
}