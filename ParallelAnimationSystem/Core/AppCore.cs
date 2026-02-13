using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Pamx.Common;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Model;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering.Data;
using ParallelAnimationSystem.Text;

namespace ParallelAnimationSystem.Core;

public class AppCore
{
    private readonly List<List<IMesh>> meshes = [];

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
    
    private readonly ConditionalWeakTable<ShapedRichText, IText> loadedTexts = [];
    
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
        
        // Load beatmap
        var beatmapData = BeatmapLoader.Load(beatmap);
        
        objectSourceManager.AttachBeatmapData(beatmapData);
        eventManager.AttachBeatmapData(beatmapData);
        themeManager.AttachBeatmapData(beatmapData);
        
        sw.Stop();
        
        // Print statistics
        logger.LogInformation("Beatmap loaded in {ElapsedMilliseconds}ms ({ObjectCount} objects)", sw.ElapsedMilliseconds, playbackObjects.Count);
    }
    
    public void ProcessFrame(float time, IDrawList drawList)
    {
        // Update stuff
        var tcs = themeManager.ComputeThemeAt(time);
        var eventState = eventManager.ComputeEventAt(time, tcs);
        var drawItems = animationPipeline.ComputeDrawItems(time, tcs);
        
        // Calculate shake vector
        const float shakeMagic1 = 123.97f;
        const float shakeMagic2 = 423.42f;
        const float shakeFrequency = 10.0f;
        
        var shake = eventState.CameraShake;
        var shakeVector = new Vector2(
            MathF.Sin(time * MathF.PI * shakeFrequency + shakeMagic1) + MathF.Sin(time * shakeFrequency * 2.0f - shakeMagic1),
            MathF.Sin(time * MathF.PI * shakeFrequency - shakeMagic2) + MathF.Sin(time * shakeFrequency * 2.0f + shakeMagic2));
        shakeVector *= shake * 0.5f;
        
        // Start queuing draw commands
        drawList.Clear();

        drawList.ClearColor = new ColorRgba(tcs.Background.R, tcs.Background.G, tcs.Background.B, 1.0f);
        drawList.CameraData = new CameraData(
            eventState.CameraPosition + shakeVector,
            eventState.CameraScale,
            eventState.CameraRotation);
        
        if (appSettings.EnablePostProcessing)
        {
            drawList.PostProcessingData = new PostProcessingData(
                Time: time, 
                ChromaticAberration: CreateChromaticAberrationData(eventState.Chroma), 
                LegacyBloom: CreateLegacyBloomData(eventState.Bloom.Intensity, beatmapFormat == BeatmapFormat.Lsb), 
                UniversalBloom: CreateUniversalBloomData(eventState.Bloom.Intensity, eventState.Bloom.Diffusion, beatmapFormat == BeatmapFormat.Vgd), 
                Vignette: CreateVignetteData(
                    eventState.Vignette.Center,
                    eventState.Vignette.Intensity,
                    eventState.Vignette.Rounded,
                    eventState.Vignette.Roundness,
                    eventState.Vignette.Smoothness,
                    eventState.Vignette.Color), 
                Gradient: CreateGradientData(
                    eventState.Gradient.Color1,
                    eventState.Gradient.Color2,
                    eventState.Gradient.Intensity,
                    eventState.Gradient.Rotation,
                    eventState.Gradient.Mode), 
                HueShift: CreateHueShiftData(eventState.Hue), 
                LensDistortion: CreateLensDistortionData(eventState.LensDistortion.Intensity, eventState.LensDistortion.Center), 
                Glitch: CreateGlitchData(eventState.Glitch.Intensity, eventState.Glitch.Speed, eventState.Glitch.Width));
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
                        
                        var color1Rgba = new ColorRgba(color1, drawItem.Opacity);
                        var color2Rgba = color1 == color2 
                            ? new ColorRgba(color2, 0.0f)
                            : new ColorRgba(color2, drawItem.Opacity);
            
                        drawList.AddMesh(mesh, transform, color1Rgba, color2Rgba, renderMode);
                    }
                }
            }
            else if (appSettings.EnableTextRendering)
            {
                if (playbackObject.Text is not null)
                {
                    if (!loadedTexts.TryGetValue(playbackObject.Text, out var text))
                    {
                        text = renderingFactory.CreateText(playbackObject.Text);
                        loadedTexts.Add(playbackObject.Text, text);
                    }
                    
                    var color1 = drawItem.Color1;
                    var color1Rgba = new ColorRgba(color1, drawItem.Opacity);
                    drawList.AddText(text, transform, color1Rgba);
                }
            }
        }
    }
    
    private static HueShiftPostProcessingData CreateHueShiftData(float hue)
    {
        return new HueShiftPostProcessingData(hue);
    }
    
    private static BloomPostProcessingData CreateLegacyBloomData(float intensity, bool enabled)
    {
        if (!enabled)
            return new BloomPostProcessingData(0f, 0f);
        
        return new BloomPostProcessingData(intensity, 7f);
    }
    
    private static BloomPostProcessingData CreateUniversalBloomData(float intensity, float diffusion, bool enabled)
    {
        if (!enabled)
            return new BloomPostProcessingData(0f, 0f);
        
        diffusion = MathUtil.MapRange(diffusion, 5f, 30f, 0f, 1f);
        return new BloomPostProcessingData(intensity, diffusion);
    }
    
    private static LensDistortionPostProcessingData CreateLensDistortionData(float intensity, Vector2 center)
    {
        return new LensDistortionPostProcessingData(intensity, center);
    }
    
    private static ChromaticAberrationPostProcessingData CreateChromaticAberrationData(float intensity)
    {
        return new ChromaticAberrationPostProcessingData(intensity);
    }
    
    private static VignettePostProcessingData CreateVignetteData(Vector2 center, float intensity, bool rounded, float roundness, float smoothness, ColorRgb color)
    {
        return new VignettePostProcessingData(center, intensity, rounded, roundness, smoothness, color);
    }
    
    private static GradientPostProcessingData CreateGradientData(ColorRgb color1, ColorRgb color2, float intensity, float rotation, GradientOverlayMode mode)
    {
        return new GradientPostProcessingData(color1, color2, intensity, rotation, mode);
    }
    
    private static GlitchPostProcessingData CreateGlitchData(float intensity, float speed, float width)
    {
        return new GlitchPostProcessingData(0.0f, 0.0f, Vector2.One);
    }
}