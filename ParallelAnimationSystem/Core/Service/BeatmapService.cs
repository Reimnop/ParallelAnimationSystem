using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Model;

namespace ParallelAnimationSystem.Core.Service;

public class BeatmapService
{
    public BeatmapData BeatmapData { get; }
    public BeatmapFormat BeatmapFormat { get; }
    
    private readonly ThemeManager themeManager;
    private readonly EventManager eventManager;
    private readonly AnimationPipeline animationPipeline;

    public BeatmapService(
        IServiceProvider serviceProvider,
        IMediaProvider mediaProvider,
        ObjectSourceManager objectSourceManager,
        EventManager eventManager,
        ThemeManager themeManager,
        AnimationPipeline animationPipeline,
        PlaybackObjectContainer playbackObjects,
        ILogger<BeatmapService> logger)
    {
        this.themeManager = themeManager;
        this.eventManager = eventManager;
        this.animationPipeline = animationPipeline;
        
        // Load beatmap
        var sw = Stopwatch.StartNew();
        
        var beatmap = mediaProvider.LoadBeatmap(out var beatmapFormat);
        BeatmapFormat = beatmapFormat;
        
        logger.LogInformation("Using beatmap format {BeatmapFormat}", BeatmapFormat);
        logger.LogInformation("Loading beatmap took {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);

        // Migrate the beatmap to the latest version of the beatmap format
        sw.Restart();
        switch (beatmapFormat)
        {
            case BeatmapFormat.Lsb:
                serviceProvider.GetRequiredService<LsMigration>().MigrateBeatmap(beatmap);
                break;
            case BeatmapFormat.Vgd:
                serviceProvider.GetRequiredService<VgMigration>().MigrateBeatmap(beatmap);
                break;
        }
        logger.LogInformation("Migrating beatmap took {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);

        // Load beatmap
        sw.Restart();
        BeatmapData = BeatmapParser.Parse(beatmap);
        logger.LogInformation("Parsing beatmap took {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);

        sw.Restart();
        objectSourceManager.AttachBeatmapData(BeatmapData);
        eventManager.AttachBeatmapData(BeatmapData);
        themeManager.AttachBeatmapData(BeatmapData);
        logger.LogInformation("Attaching beatmap data took {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
        
        logger.LogInformation("Beatmap loading complete, loaded {ObjectCount} objects", playbackObjects.Count);

        sw.Stop();
    }

    public void ProcessBeatmap(float time, out ThemeColorState themeColorState, out EventState eventState, out ReadOnlySpan<ObjectDrawItem> drawItems)
    {
        themeColorState = themeManager.ComputeThemeAt(time);
        eventState = eventManager.ComputeEventAt(time, themeColorState);
        drawItems = animationPipeline.ComputeDrawItems(time, themeColorState);
    }
}