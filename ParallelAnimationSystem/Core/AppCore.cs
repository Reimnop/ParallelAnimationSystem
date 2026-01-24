using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Pamx.Common;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Beatmap;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Text;
using TmpParser;

namespace ParallelAnimationSystem.Core;

public class AppCore
{
    private readonly List<List<IMesh>> meshes = [];
    
    private readonly AnimationRunner runner;
    private readonly FontCollection fonts;

    private readonly AppSettings appSettings;
    private readonly ResourceLoader loader;
    private readonly IRenderingFactory renderingFactory;
    private readonly ILogger<AppCore> logger;
    
    private readonly ConditionalWeakTable<BeatmapObject, Task<IText?>> textCache = new();
    
    public AppCore(
        IServiceProvider sp,
        AppSettings appSettings,
        ResourceLoader loader,
        IMediaProvider mediaProvider,
        IRenderingFactory renderingFactory,
        ILogger<AppCore> logger)
    {
        this.appSettings = appSettings;
        this.loader = loader;
        this.renderingFactory = renderingFactory;
        this.logger = logger;

        #region Mesh Loading

        {
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

        #endregion

        #region Font Loading

        {
            var inconsolata = LoadFont("Fonts/Inconsolata.tmpe");
            var arialuni = LoadFont("Fonts/Arialuni.tmpe");
            var seguisym = LoadFont("Fonts/Seguisym.tmpe");
            var code2000 = LoadFont("Fonts/Code2000.tmpe");
            var inconsolataStack = new FontStack("Inconsolata SDF", 16.0f, [inconsolata, arialuni, seguisym, code2000]);
        
            var liberationSans = LoadFont("Fonts/LiberationSans.tmpe");
            var liberationSansStack = new FontStack("LiberationSans SDF", 16.0f, [liberationSans, arialuni, seguisym, code2000]);
        
            var notoMono = LoadFont("Fonts/NotoMono.tmpe");
            var notoMonoStack = new FontStack("NotoMono SDF", 16.0f, [notoMono, arialuni, seguisym, code2000]);
            
            fonts = new FontCollection([inconsolataStack, liberationSansStack, notoMonoStack]);
        }

        #endregion
        
        // Load beatmap
        var sw = Stopwatch.StartNew();
        
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
        logger.LogInformation("Importing beatmap");
        var beatmapImporter = new BeatmapImporter(appSettings.Seed, logger);
        runner = beatmapImporter.CreateRunner(beatmap, out var statistics);
        
        sw.Stop();
        
        // Print statistics
        logger.LogInformation("Beatmap loaded in {ElapsedMilliseconds}ms, {Statistics}", sw.ElapsedMilliseconds, statistics);
    }

    private IFont LoadFont(string path)
    {
        using var stream = loader.OpenResource(path);
        if (stream is null)
            throw new InvalidOperationException($"Could not load font data at '{path}'");
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
            drawList.PostProcessingData = new PostProcessingData(
                time,
                CreateHueShiftData(runner.Hue),
                CreateBloomData(runner.Bloom.Intensity, runner.Bloom.Diffusion),
                CreateLensDistortionData(runner.LensDistortion.Intensity, runner.LensDistortion.Center),
                CreateChromaticAberrationData(runner.ChromaticAberration),
                CreateVignetteData(
                    runner.Vignette.Center,
                    runner.Vignette.Intensity,
                    runner.Vignette.Rounded,
                    runner.Vignette.Roundness,
                    runner.Vignette.Smoothness,
                    runner.Vignette.Color),
                CreateGradientData(
                    runner.Gradient.Color1,
                    runner.Gradient.Color2,
                    runner.Gradient.Intensity,
                    runner.Gradient.Rotation,
                    runner.Gradient.Mode),
                CreateGlitchData(runner.Glitch.Intensity, runner.Glitch.Speed, Vector2.Zero));
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
            
            if (beatmapObject.Shape != ObjectShape.Text)
            {
                beatmapObject.Shape.ToSeparate(out var shapeIndex, out var shapeOptionIndex);
                
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
                // Create text
                var textTask = textCache.GetValue(beatmapObject, CreateTextAsync);

                // Draw text if ready
                if (textTask is { IsCompletedSuccessfully: true, Result: not null })
                {
                    var color1 = beatmapObjectColor.Color1;
                    var color1Rgba = new ColorRgba(color1.R, color1.G, color1.B, beatmapObjectColor.Opacity);
                    drawList.AddText(textTask.Result, transform, color1Rgba);
                }
            }
        }
    }

    private Task<IText?> CreateTextAsync(BeatmapObject beatmapObject)
    {
        var task = Task.Run(() =>
        {
            if (beatmapObject.Text is null)
                return null;

            var richText = new RichText(
                beatmapObject.Text,
                "NotoMono SDF",
                beatmapObject.Origin.X switch
                {
                    -0.5f => HorizontalAlignment.Left,
                    0.5f => HorizontalAlignment.Right,
                    _ => HorizontalAlignment.Center,
                },
                beatmapObject.Origin.Y switch
                {
                    -0.5f => VerticalAlignment.Bottom,
                    0.5f => VerticalAlignment.Top,
                    _ => VerticalAlignment.Center,
                });
            var text = renderingFactory.CreateText(richText, fonts);
            return text;
        });
        
        return task;
    }
    
    protected virtual HueShiftPostProcessingData CreateHueShiftData(float hue)
    {
        return new HueShiftPostProcessingData(hue);
    }
    
    protected virtual BloomPostProcessingData CreateBloomData(float intensity, float diffusion)
    {
        diffusion = MathUtil.MapRange(diffusion, 5.0f, 30.0f, 1.0f, 10.0f);
        return new BloomPostProcessingData(intensity, diffusion);
    }
    
    protected virtual LensDistortionPostProcessingData CreateLensDistortionData(float intensity, Vector2 center)
    {
        return new LensDistortionPostProcessingData(intensity, center);
    }
    
    protected virtual ChromaticAberrationPostProcessingData CreateChromaticAberrationData(float intensity)
    {
        return new ChromaticAberrationPostProcessingData(intensity);
    }
    
    protected virtual VignettePostProcessingData CreateVignetteData(Vector2 center, float intensity, bool rounded, float roundness, float smoothness, Vector3 color)
    {
        return new VignettePostProcessingData(center, intensity, rounded, roundness, smoothness, color);
    }
    
    protected virtual GradientPostProcessingData CreateGradientData(Vector3 color1, Vector3 color2, float intensity, float rotation, GradientOverlayMode mode)
    {
        return new GradientPostProcessingData(color1, color2, intensity, rotation, mode);
    }
    
    protected virtual GlitchPostProcessingData CreateGlitchData(float intensity, float speed, Vector2 size)
    {
        return new GlitchPostProcessingData(0.0f, 0.0f, Vector2.One);
    }
}