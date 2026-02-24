using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Util;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Desktop;

public sealed class DesktopApp
{
    private readonly ConcurrentQueue<IDrawList> drawListPool = [];
    private readonly ConcurrentQueue<IDrawList> queuedDrawLists = [];

    private readonly IServiceProvider serviceProvider;
    private readonly MediaContext mediaContext;

    private volatile bool running = true;

    public DesktopApp(IServiceProvider serviceProvider, MediaContext mediaContext, IRenderingFactory renderingFactory)
    {
        this.serviceProvider = serviceProvider;
        this.mediaContext = mediaContext;

        for (var i = 0; i < 3; i++)
            drawListPool.Enqueue(renderingFactory.CreateDrawList());
    }

    public void StartApp(ulong seed)
    {
        // Set random seed
        var randomSeedService = serviceProvider.GetRequiredService<RandomSeedService>();
        randomSeedService.Seed = seed;
        
        // Load beatmap
        ReadBeatmap(out var beatmapData, out var beatmapFormat);
        
        var beatmapService = serviceProvider.GetRequiredService<BeatmapService>();
        beatmapService.LoadBeatmap(beatmapData, beatmapFormat);
        
        // Initialize core service
        var appCore = serviceProvider.GetRequiredService<AppCore>();
        
        // Play audio
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
        
        // Wait for render thread to finish
        renderThread.Join();
        
        // Stop audio
        audioPlayer.Stop();
    }

    private void StartRenderThread()
    {
        using var scope = serviceProvider.CreateScope();
        
        // Initialize renderer
        var renderer = scope.ServiceProvider.GetRequiredService<IRenderer>();
        var window = scope.ServiceProvider.GetRequiredService<IWindow>();

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
    
    private void ReadBeatmap(out string beatmapData, out BeatmapFormat beatmapFormat)
    {
        beatmapData = File.ReadAllText(mediaContext.BeatmapPath);
        
        var extension = Path.GetExtension(mediaContext.BeatmapPath).ToLowerInvariant();
        beatmapFormat = extension switch
        {
            ".lsb" => BeatmapFormat.Lsb,
            ".vgd" => BeatmapFormat.Vgd,
            _ => throw new NotSupportedException($"Unsupported beatmap format '{extension}'")
        };
    }
}