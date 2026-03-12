using System.Numerics;
using Pamx.Common;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering.Data;
using ParallelAnimationSystem.Rendering.Handle;
using BloomEffectState = ParallelAnimationSystem.Rendering.Data.BloomEffectState;
using GradientEffectState = ParallelAnimationSystem.Rendering.Data.GradientEffectState;
using VignetteEffectState = ParallelAnimationSystem.Rendering.Data.VignetteEffectState;

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
        drawList.CameraState = new CameraState
        {
            Position = eventState.CameraPosition + shakeVector,
            Scale = eventState.CameraScale,
            Rotation = eventState.CameraRotation
        };
        
        if (appSettings.EnablePostProcessing)
        {
            drawList.PostProcessingState = new PostProcessingState
            {
                Time = time,
                ChromaticAberration = new ChromaticAberrationEffectState
                {
                    Intensity = eventState.Chroma
                },
                LegacyBloom = beatmapService.BeatmapFormat == BeatmapFormat.Lsb
                    ? new BloomEffectState
                    {
                        Intensity = eventState.Bloom.Intensity,
                        Diffusion = 7f,
                        Color = new ColorRgb(1f, 1f, 1f)
                    }
                    : default,
                UniversalBloom = beatmapService.BeatmapFormat == BeatmapFormat.Vgd
                    ? new BloomEffectState
                    {
                        Intensity = eventState.Bloom.Intensity,
                        Diffusion = MathUtil.MapRange(eventState.Bloom.Diffusion, 5f, 30f, 0f, 1f),
                        Color = eventState.Bloom.Color
                    }
                    : default,
                Vignette = new VignetteEffectState
                {
                    Center = eventState.Vignette.Center,
                    Intensity = eventState.Vignette.Intensity,
                    Rounded = eventState.Vignette.Rounded,
                    Roundness = eventState.Vignette.Roundness,
                    Smoothness = eventState.Vignette.Smoothness,
                    Color = eventState.Vignette.Color,
                    Mode = beatmapService.BeatmapFormat == BeatmapFormat.Lsb 
                        ? VignetteMode.UseRoundness 
                        : VignetteMode.UseRounded
                },
                Gradient = new GradientEffectState
                {
                    Color1 = eventState.Gradient.Color1,
                    Color2 = eventState.Gradient.Color2,
                    Intensity = eventState.Gradient.Intensity,
                    Rotation = eventState.Gradient.Rotation * MathF.Tau,
                    Mode = eventState.Gradient.Mode
                },
                HueShift = new HueShiftEffectState
                {
                    Angle = eventState.Hue
                },
                LensDistortion = new LensDistortionEffectState
                {
                    Intensity = eventState.LensDistortion.Intensity,
                    Center = eventState.LensDistortion.Center
                },
                Glitch = new GlitchEffectState
                {
                    Speed = eventState.Glitch.Speed,
                    Intensity = 1.25f,
                    Amount = eventState.Glitch.Intensity,
                    StretchMultiplier = eventState.Glitch.Width
                }
            };
        }
        else
        {
            drawList.PostProcessingState = default;
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
            
                    drawList.AddMesh(mesh, transform, color1Rgba, color2Rgba, renderMode, playbackObject.GradientRotation, playbackObject.GradientScale);
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
}