using System.Diagnostics;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pamx.Ls;
using Pamx.Vg;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Model;

namespace ParallelAnimationSystem.Core.Service;

public class BeatmapService : IDisposable
{
    public BeatmapData BeatmapData { get; }
    public BeatmapFormat BeatmapFormat { get; private set; }
    
    private readonly IServiceProvider serviceProvider;
    private readonly ObjectSourceManager objectSourceManager;
    private readonly EventManager eventManager;
    private readonly ThemeManager themeManager;
    private readonly AnimationPipeline animationPipeline;
    private readonly PlaybackObjectContainer playbackObjects;
    private readonly ILogger<BeatmapService> logger;

    public BeatmapService(
        IServiceProvider serviceProvider,
        ObjectSourceManager objectSourceManager,
        EventManager eventManager,
        ThemeManager themeManager,
        AnimationPipeline animationPipeline,
        PlaybackObjectContainer playbackObjects,
        ILogger<BeatmapService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.objectSourceManager = objectSourceManager;
        this.eventManager = eventManager;
        this.themeManager = themeManager;
        this.animationPipeline = animationPipeline;
        this.playbackObjects = playbackObjects;
        this.logger = logger;

        BeatmapData = new BeatmapData();
        
        objectSourceManager.AttachBeatmapData(BeatmapData);
        eventManager.AttachBeatmapData(BeatmapData);
        themeManager.AttachBeatmapData(BeatmapData);
    }

    public void Dispose()
    {
        // Detach beatmap data from managers
        objectSourceManager.DetachBeatmapData();
        eventManager.DetachBeatmapData();
        themeManager.DetachBeatmapData();
    }

    public void LoadBeatmap(string data, BeatmapFormat format)
    {
        // Load beatmap
        var sw = Stopwatch.StartNew();

        try
        {
            BeatmapData.Clear();
            logger.LogInformation("Clearing existing beatmap data took {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);

            // Parse beatmap
            sw.Restart();
            var beatmapJson = JsonNode.Parse(data);
            if (beatmapJson is not JsonObject beatmapJsonObject)
                throw new InvalidDataException("Invalid beatmap JSON");
            logger.LogInformation("Parsing beatmap took {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);

            // Deserialize beatmap
            sw.Restart();
            var beatmap = format switch
            {
                BeatmapFormat.Lsb => LsDeserialization.DeserializeBeatmap(beatmapJsonObject),
                BeatmapFormat.Vgd => VgDeserialization.DeserializeBeatmap(beatmapJsonObject),
                _ => throw new NotSupportedException($"Unsupported beatmap format '{format}'"),
            };
            logger.LogInformation("Deserializing beatmap took {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);

            // Migrate the beatmap
            sw.Restart();
            switch (format)
            {
                case BeatmapFormat.Lsb:
                    serviceProvider.GetRequiredService<LsMigration>().MigrateBeatmap(beatmap);
                    break;
                case BeatmapFormat.Vgd:
                    serviceProvider.GetRequiredService<VgMigration>().MigrateBeatmap(beatmap);
                    break;
            }
            logger.LogInformation("Migrating beatmap took {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);

            // Import beatmap
            sw.Restart();
            BeatmapImporter.Import(beatmap, BeatmapData);
            logger.LogInformation("Importing beatmap data took {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);

            BeatmapFormat = format;

            logger.LogInformation("Beatmap loading complete, loaded {ObjectCount} objects", playbackObjects.Count);
        }
        finally
        {
            sw.Stop();
        }
    }

    public void ProcessBeatmap(float time, out ThemeColorState themeColorState, out EventState eventState, out ReadOnlySpan<ObjectDrawItem> drawItems)
    {
        themeColorState = themeManager.ComputeThemeAt(time);
        eventState = eventManager.ComputeEventAt(time, themeColorState);
        drawItems = animationPipeline.ComputeDrawItems(time, themeColorState);
    }
}