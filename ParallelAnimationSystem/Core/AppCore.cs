using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using Pamx.Common;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Animation;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Model;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering.Data;
using ParallelAnimationSystem.Text;

namespace ParallelAnimationSystem.Core;

public class AppCore
{
    private readonly List<List<IMesh>> meshes = [];
    
    private readonly FontCollection fonts;

    private readonly AppSettings appSettings;
    private readonly ResourceLoader loader;
    private readonly PlaybackObjectContainer playbackObjects;
    private readonly ObjectSourceManager objectSourceManager;
    private readonly AnimationPipeline animationPipeline;
    private readonly EventManager eventManager;
    private readonly ThemeManager themeManager;
    private readonly IRenderingFactory renderingFactory;
    private readonly ILogger<AppCore> logger;

    private readonly BeatmapFormat beatmapFormat;
    
    public AppCore(
        IServiceProvider sp,
        AppSettings appSettings,
        ResourceLoader loader,
        IMediaProvider mediaProvider,
        PlaybackObjectContainer playbackObjects,
        ObjectSourceManager objectSourceManager,
        AnimationPipeline animationPipeline,
        EventManager eventManager,
        ThemeManager themeManager,
        IRenderingFactory renderingFactory,
        ILogger<AppCore> logger)
    {
        this.appSettings = appSettings;
        this.loader = loader;
        this.playbackObjects = playbackObjects;
        this.objectSourceManager = objectSourceManager;
        this.animationPipeline = animationPipeline;
        this.eventManager = eventManager;
        this.themeManager = themeManager;
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
        var beatmap = mediaProvider.LoadBeatmap(out beatmapFormat);
        
        logger.LogInformation("Using beatmap format '{LevelFormat}'", beatmapFormat);
        
        // Migrate the beatmap to the latest version of the beatmap format
        logger.LogInformation("Migrating beatmap");
        switch (beatmapFormat) 
        {
            case BeatmapFormat.Lsb:
                sp.GetRequiredService<LsMigration>().MigrateBeatmap(beatmap);
                break;
            case BeatmapFormat.Vgd:
                sp.GetRequiredService<VgMigration>().MigrateBeatmap(beatmap);
                break;
        }
        
        logger.LogInformation("Using seed '{Seed}'", appSettings.Seed);
        
        // Load beatmap
        var beatmapData = BeatmapLoader.Load(beatmap);
        
        objectSourceManager.AttachBeatmapData(beatmapData);
        eventManager.AttachBeatmapData(beatmapData);
        themeManager.AttachBeatmapData(beatmapData);
        
        sw.Stop();
        
        // Print statistics
        logger.LogInformation("Beatmap loaded in {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
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
        // Update stuff
        var tcs = themeManager.ComputeThemeAt(time);
        var eventState = eventManager.ComputeEventAt(time, tcs);
        var drawItems = animationPipeline.ComputeDrawItems(time, tcs);
        
        // Calculate shake vector
        // const float shakeMagic1 = 123.97f;
        // const float shakeMagic2 = 423.42f;
        // const float shakeFrequency = 10.0f;
        
        // var shake = runner.Shake;
        // var shakeVector = new Vector2(
        //     MathF.Sin(time * MathF.PI * shakeFrequency + shakeMagic1) + MathF.Sin(time * shakeFrequency * 2.0f - shakeMagic1),
        //     MathF.Sin(time * MathF.PI * shakeFrequency - shakeMagic2) + MathF.Sin(time * shakeFrequency * 2.0f + shakeMagic2));
        // shakeVector *= shake * 0.5f;
        //
        // var backgroundColor = runner.BackgroundColor;
        
        // Start queuing draw commands
        drawList.Clear();
        
        // drawList.ClearColor = new ColorRgba(backgroundColor.R, backgroundColor.G, backgroundColor.B, 1.0f);
        // drawList.CameraData = new CameraData(
        //     runner.CameraPosition + shakeVector,
        //     runner.CameraScale,
        //     runner.CameraRotation);

        drawList.ClearColor = new ColorRgba(tcs.Background.R, tcs.Background.G, tcs.Background.B, 1.0f);
        drawList.CameraData = new CameraData(
            eventState.CameraPosition,
            eventState.CameraScale,
            eventState.CameraRotation);
        
        if (appSettings.EnablePostProcessing)
        {
            // drawList.PostProcessingData = new PostProcessingData(
            //     time,
            //     CreateHueShiftData(runner.Hue),
            //     CreateLegacyBloomData(runner.Bloom.Intensity, isLegacy: beatmapFormat == BeatmapFormat.Lsb),
            //     CreateUniversalBloomData(runner.Bloom.Intensity, runner.Bloom.Diffusion, isUniversal: beatmapFormat == BeatmapFormat.Vgd),
            //     CreateLensDistortionData(runner.LensDistortion.Intensity, runner.LensDistortion.Center),
            //     CreateChromaticAberrationData(runner.ChromaticAberration),
            //     CreateVignetteData(
            //         runner.Vignette.Center,
            //         runner.Vignette.Intensity,
            //         runner.Vignette.Rounded,
            //         runner.Vignette.Roundness,
            //         runner.Vignette.Smoothness,
            //         runner.Vignette.Color),
            //     CreateGradientData(
            //         runner.Gradient.Color1,
            //         runner.Gradient.Color2,
            //         runner.Gradient.Intensity,
            //         runner.Gradient.Rotation,
            //         runner.Gradient.Mode),
            //     CreateGlitchData(runner.Glitch.Intensity, runner.Glitch.Speed, Vector2.Zero));
        }
        else
        {
            drawList.PostProcessingData = default;
        }

        // Draw all alive game objects
        foreach (var drawItem in drawItems)
        {
            var transform = drawItem.Transform;

            if (!playbackObjects.TryGetItem(drawItem.ObjectIndex, out var playbackObject))
                continue;
            
            if (playbackObject.Shape != ObjectShape.Text)
            {
                playbackObject.Shape.ToSeparate(out var shapeIndex, out var shapeOptionIndex);
                
                if (shapeIndex >= 0 && shapeIndex < meshes.Count)
                {
                    var meshOptionList = meshes[shapeIndex];

                    if (shapeOptionIndex >= 0 && shapeOptionIndex < meshOptionList.Count)
                    {
                        var mesh = meshOptionList[shapeOptionIndex];
                        var renderMode = playbackObject.RenderMode;

                        var color1 = drawItem.Color1;
                        var color2 = drawItem.Color2;
                        
                        var color1Rgba = new ColorRgba(color1.R, color1.G, color1.B, drawItem.Opacity);
                        var color2Rgba = color1 == color2 
                            ? new ColorRgba(color2.R, color2.G, color2.B, 0.0f)
                            : new ColorRgba(color2.R, color2.G, color2.B, drawItem.Opacity);
            
                        drawList.AddMesh(mesh, transform, color1Rgba, color2Rgba, renderMode);
                    }
                }
            }
            else if (appSettings.EnableTextRendering)
            {
                // TODO: do text rendering
            }
        }
    }
    
    protected virtual HueShiftPostProcessingData CreateHueShiftData(float hue)
    {
        return new HueShiftPostProcessingData(hue);
    }
    
    protected virtual BloomPostProcessingData CreateLegacyBloomData(float intensity, bool isLegacy)
    {
        if (!isLegacy)
            return new BloomPostProcessingData(0f, 0f);
        
        return new BloomPostProcessingData(intensity, 7f);
    }
    
    protected virtual BloomPostProcessingData CreateUniversalBloomData(float intensity, float diffusion, bool isUniversal)
    {
        if (!isUniversal)
            return new BloomPostProcessingData(0f, 0f);
        
        diffusion = MathUtil.MapRange(diffusion, 5f, 30f, 0f, 1f);
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