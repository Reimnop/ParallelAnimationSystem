using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Text;

namespace ParallelAnimationSystem.Core;

public class BeatmapRunner
{
    private readonly List<List<IMesh>> meshes = [];
    
    private readonly AnimationRunner runner;
    private readonly List<FontStack> fonts = [];

    private readonly AppSettings appSettings;
    private readonly ResourceLoader loader;
    private readonly IRenderingFactory renderingFactory;
    private readonly ILogger<BeatmapRunner> logger;
    
    public BeatmapRunner(
        IServiceProvider sp,
        AppSettings appSettings,
        ResourceLoader loader,
        IMediaProvider mediaProvider,
        IRenderingFactory renderingFactory,
        ILogger<BeatmapRunner> logger)
    {
        this.appSettings = appSettings;
        this.loader = loader;
        this.renderingFactory = renderingFactory;
        this.logger = logger;
        
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
        switch (format) 
        {
            case BeatmapFormat.Lsb:
                sp.GetRequiredService<LsMigration>().MigrateBeatmap(beatmap);
                break;
            case BeatmapFormat.Vgd:
                sp.GetRequiredService<VgMigration>().MigrateBeatmap(beatmap);
                break;
        }
        
        logger.LogInformation("Using seed '{Seed}'", appSettings.Seed);
        
        // Create animation runner
        logger.LogInformation("Initializing animation runner");
        var beatmapImporter = new BeatmapImporter(appSettings.Seed, logger);
        runner = beatmapImporter.CreateRunner(beatmap);
    }

    private void RegisterMeshes()
    {
        logger.LogInformation("Registering meshes");
        
        meshes.Add([
            renderingFactory.CreateMesh(PaAssets.SquareFilledVertices, PaAssets.SquareFilledIndices),
            renderingFactory.CreateMesh(PaAssets.SquareOutlineVertices, PaAssets.SquareOutlineIndices),
            renderingFactory.CreateMesh(PaAssets.SquareOutlineThinVertices, PaAssets.SquareOutlineThinIndices),
        ]);
        
        meshes.Add([
            renderingFactory.CreateMesh(PaAssets.CircleFilledVertices, PaAssets.CircleFilledIndices),
            renderingFactory.CreateMesh(PaAssets.CircleOutlineVertices, PaAssets.CircleOutlineIndices),
            renderingFactory.CreateMesh(PaAssets.CircleHalfVertices, PaAssets.CircleHalfIndices),
            renderingFactory.CreateMesh(PaAssets.CircleHalfOutlineVertices, PaAssets.CircleHalfOutlineIndices),
            renderingFactory.CreateMesh(PaAssets.CircleOutlineThinVertices, PaAssets.CircleOutlineThinIndices),
            renderingFactory.CreateMesh(PaAssets.CircleQuarterVertices, PaAssets.CircleQuarterIndices),
            renderingFactory.CreateMesh(PaAssets.CircleQuarterOutlineVertices, PaAssets.CircleQuarterOutlineIndices),
            renderingFactory.CreateMesh(PaAssets.CircleHalfQuarterVertices, PaAssets.CircleHalfQuarterIndices),
            renderingFactory.CreateMesh(PaAssets.CircleHalfQuarterOutlineVertices, PaAssets.CircleHalfQuarterOutlineIndices),
        ]);
        
        meshes.Add([
            renderingFactory.CreateMesh(PaAssets.TriangleFilledVertices, PaAssets.TriangleFilledIndices),
            renderingFactory.CreateMesh(PaAssets.TriangleOutlineVertices, PaAssets.TriangleOutlineIndices),
            renderingFactory.CreateMesh(PaAssets.TriangleRightFilledVertices, PaAssets.TriangleRightFilledIndices),
            renderingFactory.CreateMesh(PaAssets.TriangleRightOutlineVertices, PaAssets.TriangleRightOutlineIndices),
        ]);
        
        meshes.Add([
            renderingFactory.CreateMesh(PaAssets.ArrowVertices, PaAssets.ArrowIndices),
            renderingFactory.CreateMesh(PaAssets.ArrowHeadVertices, PaAssets.ArrowHeadIndices),
        ]);
        
        meshes.Add([]);
        
        meshes.Add([
            renderingFactory.CreateMesh(PaAssets.HexagonFilledVertices, PaAssets.HexagonFilledIndices),
            renderingFactory.CreateMesh(PaAssets.HexagonOutlineVertices, PaAssets.HexagonOutlineIndices),
            renderingFactory.CreateMesh(PaAssets.HexagonOutlineThinVertices, PaAssets.HexagonOutlineThinIndices),
            renderingFactory.CreateMesh(PaAssets.HexagonHalfVertices, PaAssets.HexagonHalfIndices),
            renderingFactory.CreateMesh(PaAssets.HexagonHalfOutlineVertices, PaAssets.HexagonHalfOutlineIndices),
            renderingFactory.CreateMesh(PaAssets.HexagonHalfOutlineThinVertices, PaAssets.HexagonHalfOutlineThinIndices),
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

    private IFont ReadFont(string path)
    {
        using var stream = loader.OpenResource(path);
        if (stream is null)
            throw new InvalidOperationException($"Could not load font data '{path}'");
        return renderingFactory.CreateFont(stream);
    }
    
    public void ProcessFrame(float time, IDrawList drawList)
    {
        // Update runner
        runner.ProcessFrame(time, appSettings.WorkerCount);
        
        // Calculate shake vector
        const float shakeMagic1 = 123.97f;
        const float shakeMagic2 = 423.42f;
        const float shakeFrequency = 10.0f;
        
        var shake = runner.Shake;
        var shakeVector = new Vector2(
            MathF.Sin(time * MathF.PI * shakeFrequency + shakeMagic1) + MathF.Sin(time * shakeFrequency * 2.0f - shakeMagic1),
            MathF.Sin(time * MathF.PI * shakeFrequency - shakeMagic2) + MathF.Sin(time * shakeFrequency * 2.0f + shakeMagic2));
        shakeVector *= shake * 0.5f;
        
        var backgroundColor = runner.BackgroundColor;
        
        // Start queuing draw commands
        drawList.Clear();
        
        drawList.ClearColor = new ColorRgba(backgroundColor.R, backgroundColor.G, backgroundColor.B, 1.0f);
        drawList.CameraData = new CameraData(
            runner.CameraPosition + shakeVector,
            runner.CameraScale,
            runner.CameraRotation);
        
        if (appSettings.EnablePostProcessing)
        {
            var bloomData = runner.Bloom;
            var hue = runner.Hue;
            var lensDistortionData = runner.LensDistortion;
            var vignetteData = runner.Vignette;
            var gradientData = runner.Gradient;
            var bloomDiffusion01 = MathUtil.MapRange(bloomData.Diffusion, 5.0f, 30.0f, 0.0f, 1.0f);
            // var glitchIntensity = runner.Glitch.Intensity;
            // var glitchSpeed = MathHelper.MapRange(runner.Glitch.Speed, 0.0f, 2.0f, 7.0f, 40.0f);
            // var glitchSize = new Vector2(MathHelper.MapRange(runner.Glitch.Width, 0.0f, 1.0f, 0.02f, 1.0f), 0.015f);

            drawList.PostProcessingData = new PostProcessingData(
                time,
                hue,
                bloomData.Intensity,
                bloomDiffusion01,
                lensDistortionData.Intensity, new Vector2(lensDistortionData.Center.X, lensDistortionData.Center.Y),
                runner.ChromaticAberration,
                new Vector2(vignetteData.Center.X, vignetteData.Center.Y), vignetteData.Intensity, vignetteData.Rounded, vignetteData.Roundness, vignetteData.Smoothness, vignetteData.Color,
                gradientData.Color1, gradientData.Color2, gradientData.Intensity, gradientData.Rotation * MathF.PI * 2.0f, gradientData.Mode,
                0.0f, 0.0f, Vector2.One); // glitchIntensity, glitchSpeed, glitchSize
        }
        else
        {
            drawList.PostProcessingData = default;
        }

        // Draw all alive game objects
        foreach (var perFrameObjectData in runner.PerFrameData)
        {
            var transform = perFrameObjectData.Transform;
            var beatmapObjectColor = perFrameObjectData.Color;

            var beatmapObject = perFrameObjectData.BeatmapObject;
            Debug.Assert(beatmapObject is not null);
            
            var shapeIndex = (int) beatmapObject.Shape & 0xffff;
            var shapeOptionIndex = (int) beatmapObject.Shape >> 16;
            
            if (shapeIndex != 4) // 4 is text
            {
                if (shapeIndex >= 0 && shapeIndex < meshes.Count)
                {
                    var meshOptionList = meshes[shapeIndex];

                    if (shapeOptionIndex >= 0 && shapeOptionIndex < meshOptionList.Count)
                    {
                        var mesh = meshOptionList[shapeOptionIndex];
                        var renderMode = beatmapObject.RenderMode;
                        
                        var color1 = beatmapObjectColor.Color1;
                        var color2 = beatmapObjectColor.Color2;
                        
                        var color1Rgba = new ColorRgba(color1.R, color1.G, color1.B, beatmapObjectColor.Opacity);
                        var color2Rgba = color1 == color2 
                            ? new ColorRgba(color2.R, color2.G, color2.B, 0.0f)
                            : new ColorRgba(color2.R, color2.G, color2.B, beatmapObjectColor.Opacity);
            
                        drawList.AddMesh(mesh, transform, color1Rgba, color2Rgba, renderMode);
                    }
                }
            }
            else if (appSettings.EnableTextRendering)
            {
                // TODO: Handle text rendering
                // if (cachedTextHandles.TryGetValue(gameObject, out var task) && task.IsCompleted)
                //     drawList.AddText(task.Result, transform, color1);
            }
        }
    }
}