using System.Diagnostics;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Pamx.Ls;
using Pamx.Vg;
using ParallelAnimationSystem.Audio;
using ParallelAnimationSystem.Audio.Stream;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Rendering.TextProcessing;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core;

public class App(IAppSettings appSettings, IMediaProvider mediaProvider, IResourceManager resourceManager, IRenderer renderer, AudioSystem audio, ILogger<App> logger) : IDisposable
{
    private readonly List<List<IMeshHandle>> meshes = [];
    
    private AnimationRunner? runner;
    private AudioPlayer? audioPlayer;

    private readonly Dictionary<GameObject, Task<ITextHandle>> cachedTextHandles = [];
    private readonly List<FontStack> fonts = [];

    private double time;
    private double lastAudioTime;
    
    public void Initialize()
    {
        // Register all meshes
        RegisterMeshes();
        
        // Register all fonts
        RegisterFonts();
        
        // Load beatmap
        logger.LogInformation("Loading beatmap");
        var beatmap = mediaProvider.LoadBeatmap(out var format);
        
        logger.LogInformation("Using beatmap format '{LevelFormat}'", format);
        
        // Migrate the beatmap to the latest version of the beatmap format
        logger.LogInformation("Migrating beatmap");
        if (format == BeatmapFormat.Lsb)
            LsMigration.MigrateBeatmap(beatmap);
        else
            VgMigration.MigrateBeatmap(beatmap);
        
        logger.LogInformation("Using seed '{Seed}'", appSettings.Seed);
        
        // Create animation runner
        logger.LogInformation("Initializing animation runner");
        var beatmapImporter = new BeatmapImporter(appSettings.Seed, logger);
        runner = beatmapImporter.CreateRunner(beatmap, format == BeatmapFormat.Lsb);

        if (appSettings.EnableTextRendering)
        {
            logger.LogWarning("Experimental text rendering is enabled! Things MIGHT break!");
            
            runner.ObjectSpawned += (_, go) =>
            {
                if (go.ShapeIndex != 4)
                    return;
                if (string.IsNullOrWhiteSpace(go.Text))
                    return;
                var task = Task.Run(() => renderer.CreateText(go.Text, fonts, "NotoMono SDF", go.HorizontalAlignment, go.VerticalAlignment));
                cachedTextHandles.Add(go, task);
            };
            
            runner.ObjectKilled += (_, go) =>
            {
                if (go.ShapeIndex != 4)
                    return;
                cachedTextHandles.Remove(go);
            };
        }
        
        logger.LogInformation("Loaded {ObjectCount} objects", runner.ObjectCount);
        
        // Load audio
        logger.LogInformation("Loading audio");
        
        using var stream = mediaProvider.LoadAudio();
        using var audioStream = new VorbisAudioStream(stream);
        audioPlayer = audio.CreatePlayer(audioStream);
        audioPlayer.Pitch = appSettings.Speed;
    }

    private void RegisterMeshes()
    {
        logger.LogInformation("Registering meshes");
        
        meshes.Add([
            renderer.RegisterMesh(PaAssets.SquareFilledVertices, PaAssets.SquareFilledIndices),
            renderer.RegisterMesh(PaAssets.SquareOutlineVertices, PaAssets.SquareOutlineIndices),
            renderer.RegisterMesh(PaAssets.SquareOutlineThinVertices, PaAssets.SquareOutlineThinIndices),
        ]);
        
        meshes.Add([
            renderer.RegisterMesh(PaAssets.CircleFilledVertices, PaAssets.CircleFilledIndices),
            renderer.RegisterMesh(PaAssets.CircleOutlineVertices, PaAssets.CircleOutlineIndices),
            renderer.RegisterMesh(PaAssets.CircleHalfVertices, PaAssets.CircleHalfIndices),
            renderer.RegisterMesh(PaAssets.CircleHalfOutlineVertices, PaAssets.CircleHalfOutlineIndices),
            renderer.RegisterMesh(PaAssets.CircleOutlineThinVertices, PaAssets.CircleOutlineThinIndices),
            renderer.RegisterMesh(PaAssets.CircleQuarterVertices, PaAssets.CircleQuarterIndices),
            renderer.RegisterMesh(PaAssets.CircleQuarterOutlineVertices, PaAssets.CircleQuarterOutlineIndices),
            renderer.RegisterMesh(PaAssets.CircleHalfQuarterVertices, PaAssets.CircleHalfQuarterIndices),
            renderer.RegisterMesh(PaAssets.CircleHalfQuarterOutlineVertices, PaAssets.CircleHalfQuarterOutlineIndices),
        ]);
        
        meshes.Add([
            renderer.RegisterMesh(PaAssets.TriangleFilledVertices, PaAssets.TriangleFilledIndices),
            renderer.RegisterMesh(PaAssets.TriangleOutlineVertices, PaAssets.TriangleOutlineIndices),
            renderer.RegisterMesh(PaAssets.TriangleRightFilledVertices, PaAssets.TriangleRightFilledIndices),
            renderer.RegisterMesh(PaAssets.TriangleRightOutlineVertices, PaAssets.TriangleRightOutlineIndices),
        ]);
        
        meshes.Add([
            renderer.RegisterMesh(PaAssets.ArrowVertices, PaAssets.ArrowIndices),
            renderer.RegisterMesh(PaAssets.ArrowHeadVertices, PaAssets.ArrowHeadIndices),
        ]);
        
        meshes.Add([]);
        
        meshes.Add([
            renderer.RegisterMesh(PaAssets.HexagonFilledVertices, PaAssets.HexagonFilledIndices),
            renderer.RegisterMesh(PaAssets.HexagonOutlineVertices, PaAssets.HexagonOutlineIndices),
            renderer.RegisterMesh(PaAssets.HexagonOutlineThinVertices, PaAssets.HexagonOutlineThinIndices),
            renderer.RegisterMesh(PaAssets.HexagonHalfVertices, PaAssets.HexagonHalfIndices),
            renderer.RegisterMesh(PaAssets.HexagonHalfOutlineVertices, PaAssets.HexagonHalfOutlineIndices),
            renderer.RegisterMesh(PaAssets.HexagonHalfOutlineThinVertices, PaAssets.HexagonHalfOutlineThinIndices),
        ]);
    }
    
    private void RegisterFonts()
    {
        logger.LogInformation("Registering fonts");

        var inconsolata = ReadFont("Fonts/Inconsolata.tmpe");
        var arialuni = ReadFont("Fonts/Arialuni.tmpe");
        var seguisym = ReadFont("Fonts/Seguisym.tmpe");
        var code2000 = ReadFont("Fonts/Code2000.tmpe");
        fonts.Add(new FontStack("Inconsolata SDF", 16.0f, [inconsolata, arialuni, seguisym, code2000]));
        
        var liberationSans = ReadFont("Fonts/LiberationSans.tmpe");
        fonts.Add(new FontStack("LiberationSans SDF", 16.0f, [liberationSans, arialuni, seguisym, code2000]));
        
        var notoMono = ReadFont("Fonts/NotoMono.tmpe");
        fonts.Add(new FontStack("NotoMono SDF", 16.0f, [notoMono, arialuni, seguisym, code2000]));
    }

    private IFontHandle ReadFont(string path)
    {
        using var stream = resourceManager.LoadResource(path);
        if (stream is null)
            throw new InvalidOperationException($"Failed to load font '{path}'");
        return renderer.RegisterFont(stream);
    }

    public void Run()
    {
        audioPlayer?.Play();
    }
    
    public bool ProcessFrame(double delta)
    {
        Debug.Assert(runner is not null);
        Debug.Assert(audioPlayer is not null);

        if (renderer.QueuedDrawListCount > 2)
            return false;
        
        var time = CalculateTime(audioPlayer, delta);
        
        // Update runner
        runner.Process((float) time, appSettings.WorkerCount);
        
        // Start queueing up draw data
        var bloomData = runner.Bloom;
        var drawList = renderer.GetDrawList();
        
        drawList.ClearColor = runner.BackgroundColor;
        drawList.CameraData = new CameraData(
            runner.CameraPosition,
            runner.CameraScale,
            runner.CameraRotation);
        drawList.PostProcessingData = new PostProcessingData(
            runner.Hue,
            bloomData.Intensity / (bloomData.Intensity + 2.0f),
            bloomData.Diffusion / (bloomData.Diffusion + 2.0f));

        // Draw all alive game objects
        foreach (var gameObject in runner.AliveGameObjects)
        {
            var transform = gameObject.CachedTransform;
            var z = gameObject.Depth;
            var color1 = gameObject.CachedThemeColor.Item1;
            var color2 = gameObject.CachedThemeColor.Item2;
            
            if (gameObject.ShapeIndex != 4) // 4 is text
            {
                var mesh = meshes[gameObject.ShapeIndex][gameObject.ShapeOptionIndex];
                var renderMode = gameObject.RenderMode;
            
                if (color1 == color2)
                    color2.W = 0.0f;
            
                drawList.AddMesh(mesh, transform, color1, color2, z, renderMode);
            }
            else
            {
                if (cachedTextHandles.TryGetValue(gameObject, out var task) && task.IsCompleted)
                    drawList.AddText(task.Result, transform, color1, z);
            }
        }
        
        // Submit our draw list
        renderer.SubmitDrawList(drawList);
        
        return true;
    }

    private double CalculateTime(AudioPlayer audioPlayer, double delta)
    {
        if (!audioPlayer.Playing)
            return time;
        
        var currentAudioTime = audioPlayer.Position.TotalSeconds;
        
        // If audio hasn't updated, we estimate the time using the last known time
        if (currentAudioTime == lastAudioTime)
        {
            time += delta * audioPlayer.Pitch;
        }
        else
        {
            time = currentAudioTime;
            lastAudioTime = currentAudioTime;
        }
        
        return time;
    }

    public void Dispose()
    {
        audioPlayer?.Dispose();
    }
}