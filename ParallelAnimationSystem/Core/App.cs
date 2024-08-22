using System.Diagnostics;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using NAudio.Utils;
using NAudio.Vorbis;
using NAudio.Wave;
using Pamx.Ls;
using Pamx.Vg;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem.Core;

public class App(Renderer renderer, ILogger<App> logger)
{
    private readonly List<List<MeshHandle>> meshes = [];
    
    private AnimationRunner? runner;
    private VorbisWaveReader? vorbisWaveReader;
    private WaveOutEvent? waveOutEvent;
    
    public void Initialize(string levelPath, string audioPath, LevelFormat format)
    {
        // Register all meshes
        RegisterMeshes();
        
        // Read animations from file
        logger.LogInformation("Reading level file");
        var json = File.ReadAllText(levelPath);
        var jsonNode = JsonNode.Parse(json);
        if (jsonNode is not JsonObject jsonObject)
            throw new InvalidOperationException("Invalid JSON object");
        
        var beatmap = format == LevelFormat.Lsb 
            ? LsDeserialization.DeserializeBeatmap(jsonObject) 
            : VgDeserialization.DeserializeBeatmap(jsonObject);
        
        // Migrate the beatmap to the latest version of the beatmap format
        if (format == LevelFormat.Lsb)
            LsMigration.MigrateBeatmap(beatmap);
        
        // Create animation runner
        runner = BeatmapImporter.CreateRunner(beatmap);
        
        logger.LogInformation("Loaded {ObjectCount} objects", runner.ObjectCount);
        
        // Load audio
        logger.LogInformation("Loading audio");
        vorbisWaveReader = new VorbisWaveReader(audioPath);
        waveOutEvent = new WaveOutEvent();
        waveOutEvent.Init(vorbisWaveReader);
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
    
    public async Task RunAsync()
    {
        Debug.Assert(runner is not null);
        Debug.Assert(vorbisWaveReader is not null);
        Debug.Assert(waveOutEvent is not null);
        
        // Wait until renderer is initialized
        while (!renderer.Initialized)
            await Task.Delay(50);
        
        // Play audio
        waveOutEvent.Play();
        
        // Start loop
        var stopwatch = Stopwatch.StartNew();

        var lastTime = 0.0;
        while (!renderer.Exiting)
        {
            var currentTime = stopwatch.Elapsed.TotalSeconds;
            var delta = currentTime - lastTime;
            lastTime = currentTime;
            await ProcessFrameAsync(delta);
        }
    }
    
    private async Task ProcessFrameAsync(double delta)
    {
        Debug.Assert(runner is not null);
        Debug.Assert(vorbisWaveReader is not null);
        Debug.Assert(waveOutEvent is not null);
        
        // Update runner
        var time = (float) waveOutEvent.GetPositionTimeSpan().TotalSeconds;
        await runner.ProcessAsync(time);
        
        // Start queueing up draw data
        var drawList = new DrawList
        {
            ClearColor = runner.BackgroundColor,
            CameraData = new CameraData(
                runner.CameraPosition,
                runner.CameraScale,
                runner.CameraRotation)
        };

        // Draw all alive game objects
        foreach (var gameObject in runner.AliveGameObjects)
        {
            var mesh = meshes[gameObject.ShapeIndex][gameObject.ShapeOptionIndex];
            var transform = gameObject.CachedTransform;
            var z = gameObject.Depth;
            var color = gameObject.CachedThemeColor.Item1;
            
            drawList.AddMesh(mesh, transform, z, color);
        }
        
        // Submit our draw list
        await SubmitDrawListAsync(drawList);
    }

    private async Task SubmitDrawListAsync(DrawList drawList)
    {
        // Wait until we have less than 2 draw lists queued
        while (renderer.QueuedDrawListCount > 2)
            await Task.Yield(); // We use a much smaller delay here, as we want to render as soon as possible
        
        // Submit draw list
        renderer.SubmitDrawList(drawList);
    }
}