using System.Numerics;
using Pamx.Common;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering.Data;
using ParallelAnimationSystem.Rendering.Handle;

namespace ParallelAnimationSystem.Core;

public class AppCore(
    AppSettings appSettings,
    PlaybackObjectContainer playbackObjects,
    MeshService meshService,
    BeatmapService beatmapService,
    TextCacheService textCacheService,
    MeshCacheService meshCacheService,
    IRenderQueue renderQueue)
{
    public void ProcessFrame(float time)
    {
        beatmapService.ProcessBeatmap(time, out var themeColorState, out var eventState, out var drawItems);
        
        // Calculate shake vector
        const float shakeMagic1 = 123.97f;
        const float shakeMagic2 = 423.42f;
        const float shakeFrequency = 10.0f;
        
        var shake = eventState.CameraShake;
        var shakeVector = new Vector2(
            MathF.Sin(time * MathF.PI * shakeFrequency + shakeMagic1) + MathF.Sin(time * shakeFrequency * 2.0f - shakeMagic1),
            MathF.Sin(time * MathF.PI * shakeFrequency - shakeMagic2) + MathF.Sin(time * shakeFrequency * 2.0f + shakeMagic2));
        shakeVector *= shake * 0.5f;
        
        // Get a draw list from the render queue
        var drawList = renderQueue.GetDrawList();
        
        // Start queuing draw commands
        drawList.ClearColor = new ColorRgba(themeColorState.Background);
        drawList.CameraData = new CameraData(
            eventState.CameraPosition + shakeVector,
            eventState.CameraScale,
            eventState.CameraRotation);
        
        if (appSettings.EnablePostProcessing)
        {
            drawList.PostProcessingData = new PostProcessingData(
                Time: time, 
                ChromaticAberration: CreateChromaticAberrationData(eventState.Chroma), 
                LegacyBloom: CreateLegacyBloomData(eventState.Bloom.Intensity, beatmapService.BeatmapFormat == BeatmapFormat.Lsb), 
                UniversalBloom: CreateUniversalBloomData(eventState.Bloom.Intensity, eventState.Bloom.Diffusion, beatmapService.BeatmapFormat == BeatmapFormat.Vgd), 
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
        foreach (ref var drawItem in drawItems)
        {
            var transform = drawItem.Transform;
            
            // check if scale is zero, if so skip rendering this object
            var scaleXSquared = transform.M11 * transform.M11 + transform.M12 * transform.M12;
            var scaleYSquared = transform.M21 * transform.M21 + transform.M22 * transform.M22;
            if (scaleXSquared < 0.001f || scaleYSquared < 0.001f)
                continue;

            if (!playbackObjects.TryGetItem(drawItem.ObjectIndex, out var playbackObject))
                continue;
            
            if (playbackObject.Shape != ObjectShape.Text)
            {
                if (TryGetMesh(playbackObject, drawItem.ObjectIndex, out var mesh))
                {
                    var renderMode = playbackObject.RenderMode;
                        
                    var color1Rgba = new ColorRgba(drawItem.Color1, drawItem.Opacity);
                    var color2Rgba = drawItem.Color1 == drawItem.Color2 
                        ? new ColorRgba(drawItem.Color2, 0.0f)
                        : new ColorRgba(drawItem.Color2, drawItem.Opacity);
            
                    drawList.AddMesh(mesh, transform, color1Rgba, color2Rgba, renderMode);
                }
            }
            else if (appSettings.EnableTextRendering)
            {
                if (textCacheService.TryGetText(drawItem.ObjectIndex, out var textHandle))
                {
                    var color1 = drawItem.Color1;
                    var color1Rgba = new ColorRgba(color1, drawItem.Opacity);
                    drawList.AddText(textHandle, transform, color1Rgba);
                }
            }
        }
        
        // Submit the draw list to the render queue
        renderQueue.SubmitDrawList(drawList);
    }

    private bool TryGetMesh(PlaybackObject playbackObject, int objectIndex, out MeshHandle meshHandle)
    {
        playbackObject.Shape.ToSeparate(out var shapeIndex, out var shapeOptionIndex);
        
        // look in main mesh service first
        if (meshService.TryGetMeshForShape(shapeIndex, shapeOptionIndex, out meshHandle))
            return true;
        
        // then look in mesh cache if shape is custom
        if (playbackObject.Shape 
            is ObjectShape.SquareCustom 
            or ObjectShape.CircleCustom
            or ObjectShape.TriangleCustom
            or ObjectShape.Custom
            or ObjectShape.HexagonCustom)
            if (meshCacheService.TryGetMesh(objectIndex, out meshHandle))
                return true;
        
        meshHandle = default;
        return false;
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