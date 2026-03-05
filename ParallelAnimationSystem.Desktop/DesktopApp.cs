using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Desktop;

public sealed class DesktopApp
{
    private readonly DrawList drawList = new();
    private readonly IServiceProvider serviceProvider;

    public DesktopApp(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
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
        var renderer = sp.GetRequiredService<IRenderer>();
        var window = sp.GetRequiredService<IWindow>();
        
        // Play audio
        using var audioPlayer = AudioPlayer.Load(audioPath);
        audioPlayer.Play();
        
        // Start the main thread
        while (!window.ShouldClose)
        {
            window.PollEvents();
            
            // Populate the draw list
            appCore.ProcessFrame((float) audioPlayer.Position, drawList);
            
            // Render the frame
            renderer.ProcessFrame(drawList);
            
            // Reset draw list for the next frame
            drawList.Reset();
        }
        
        // Stop audio
        audioPlayer.Stop();
    }
}