using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Desktop;

public sealed class DesktopApp
{
    private readonly ConcurrentQueue<IDrawList> drawListPool = [];
    private readonly ConcurrentQueue<IDrawList> queuedDrawLists = [];

    private readonly IServiceProvider serviceProvider;
    private readonly MediaContext mediaContext;

    private bool running = true;

    public DesktopApp(IServiceProvider serviceProvider, MediaContext mediaContext, IRenderingFactory renderingFactory)
    {
        this.serviceProvider = serviceProvider;
        this.mediaContext = mediaContext;

        for (var i = 0; i < 3; i++)
            drawListPool.Enqueue(renderingFactory.CreateDrawList());
    }

    public void StartApp()
    {
        // Initialize app core
        var appCore = serviceProvider.InitializeAppCore();
        
        // Create audio player
        using var audioPlayer = AudioPlayer.Load(mediaContext.AudioPath);
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
    }

    private void StartRenderThread()
    {
        var renderer = serviceProvider.InitializeRenderer();
        
        var window = serviceProvider.GetRequiredService<IWindow>();

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