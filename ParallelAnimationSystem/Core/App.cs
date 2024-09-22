using System.Diagnostics;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using OpenTK.Mathematics;
using Pamx.Ls;
using Pamx.Vg;
using ParallelAnimationSystem.Audio;
using ParallelAnimationSystem.Audio.Stream;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core;

public class App(Options options, Renderer renderer, AudioSystem audio, ILogger<App> logger) : IDisposable
{
    private static readonly Matrix3 TextScale = MathUtil.CreateScale(Vector2.One * 3.0f / 32.0f);
    
    private readonly List<List<MeshHandle>> meshes = [];
    
    private AnimationRunner? runner;
    private AudioPlayer? audioPlayer;

    private readonly Dictionary<GameObject, Task<TextHandle>> cachedTextHandles = [];
    private FontHandle fontInconsolata;
    
    private bool shuttingDown;

    private double time;
    private double lastAudioTime;
    
    public void Initialize()
    {
        // Register all meshes
        RegisterMeshes();
        
        // Register all fonts
        RegisterFonts();
        
        // Read animations from file
        logger.LogInformation("Reading beatmap file from '{LevelPath}'", options.LevelPath);
        var json = File.ReadAllText(options.LevelPath);
        var jsonNode = JsonNode.Parse(json);
        if (jsonNode is not JsonObject jsonObject)
            throw new InvalidOperationException("Invalid JSON object");

        // Determine level format
        var format = options.Format ?? Path.GetExtension(options.LevelPath) switch
        {
            ".lsb" => LevelFormat.Lsb,
            ".vgd" => LevelFormat.Vgd,
            _ => throw new ArgumentException("Unknown level file format")
        };
        logger.LogInformation("Using beatmap format '{LevelFormat}'", format);
        
        // Deserialize beatmap
        logger.LogInformation("Deserializing beatmap");
        var beatmap = format == LevelFormat.Lsb 
            ? LsDeserialization.DeserializeBeatmap(jsonObject) 
            : VgDeserialization.DeserializeBeatmap(jsonObject);
        
        // Migrate the beatmap to the latest version of the beatmap format
        logger.LogInformation("Migrating beatmap");
        if (format == LevelFormat.Lsb)
            LsMigration.MigrateBeatmap(beatmap);
        else
            VgMigration.MigrateBeatmap(beatmap);

        var seed = options.Seed < 0
            ? (ulong) DateTimeOffset.Now.ToUnixTimeMilliseconds()
            : (ulong) options.Seed;
        
        logger.LogInformation("Using seed '{Seed}'", seed);
        
        // Create animation runner
        logger.LogInformation("Initializing animation runner");
        var beatmapImporter = new BeatmapImporter(seed, logger);
        runner = beatmapImporter.CreateRunner(beatmap);
        runner.ObjectSpawned += (_, go) =>
        {
            if (go.ShapeIndex != 4)
                return;
            if (string.IsNullOrWhiteSpace(go.Text))
                return;
            
            var task = Task.Run(() => renderer.CreateText(go.Text, fontInconsolata));
            cachedTextHandles.Add(go, task);
        };
        runner.ObjectKilled += (_, go) =>
        {
            if (go.ShapeIndex != 4)
                return;
            cachedTextHandles.Remove(go);
        };
        
        logger.LogInformation("Loaded {ObjectCount} objects", runner.ObjectCount);
        
        // Load audio
        logger.LogInformation("Loading audio from '{AudioPath}'", options.AudioPath);
        
        // TODO: When we have streaming audio, don't dispose the stream here
        using var audioStream = new VorbisAudioStream(options.AudioPath);
        audioPlayer = audio.CreatePlayer(audioStream);
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
        
        using (var streamInconsolata = ResourceUtil.ReadAsStream("Resources.Fonts.Inconsolata.tmpe"))
            fontInconsolata = renderer.RegisterFont(streamInconsolata, 16.0f);
    }
    
    public void Run()
    {
        Debug.Assert(runner is not null);
        Debug.Assert(audioPlayer is not null);
        
        // Play audio
        audioPlayer.Play();
        
        // Start loop
        var stopwatch = Stopwatch.StartNew();

        var lastTime = 0.0;
        while (!shuttingDown)
        {
            var currentTime = stopwatch.Elapsed.TotalSeconds;
            var delta = currentTime - lastTime;
            lastTime = currentTime;
            ProcessFrame(delta);
        }
    }
    
    private void ProcessFrame(double delta)
    {
        Debug.Assert(runner is not null);
        Debug.Assert(audioPlayer is not null);
        
        var time = CalculateTime(audioPlayer, delta);
        
        // Update runner
        runner.Process((float) time, options.WorkerCount);
        
        // Start queueing up draw data
        var bloomData = runner.Bloom;
        var drawList = new DrawList
        {
            ClearColor = runner.BackgroundColor,
            CameraData = new CameraData(
                runner.CameraPosition,
                runner.CameraScale,
                runner.CameraRotation),
            PostProcessingData = new PostProcessingData(
                runner.Hue,
                bloomData.Intensity / (bloomData.Intensity + 1.0f),
                bloomData.Diffusion / (bloomData.Diffusion + 1.0f)), 
        };

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
                    drawList.AddText(task.Result, TextScale * transform, color1, z);
            }
        }
        
        // Submit our draw list
        SubmitDrawList(drawList);
    }

    private double CalculateTime(AudioPlayer audioPlayer, double delta)
    {
        if (!audioPlayer.Playing)
            return time;
        
        var currentAudioTime = audioPlayer.Position.TotalSeconds;
        
        // If audio hasn't updated, we estimate the time using the last known time
        if (currentAudioTime == lastAudioTime)
        {
            time += delta;
        }
        else
        {
            time = currentAudioTime;
            lastAudioTime = currentAudioTime;
        }
        
        return time;
    }

    private void SubmitDrawList(DrawList drawList)
    {
        // If we already have more than 2 draw lists queued, wait until we don't
        while (renderer.QueuedDrawListCount > 2 && !shuttingDown)
            Thread.Yield();
        
        // Return if we are shutting down
        if (shuttingDown)
            return;
        
        // Submit draw list
        renderer.SubmitDrawList(drawList);
    }
    
    public void Shutdown()
    {
        shuttingDown = true;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        
        shuttingDown = true;
        audioPlayer?.Dispose();
    }
}