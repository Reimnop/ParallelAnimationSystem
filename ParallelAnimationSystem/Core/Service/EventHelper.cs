using System.Numerics;
using Pamx.Events;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Core.Service;

public static class EventHelper
{
    public static IEnumerable<BakedKeyframe<T>> ResolveGenericKeyframes<T>(KeyframeList<Data.Keyframe<T>> eventsCameraPosition)
    {
        foreach (var eventKeyframe in eventsCameraPosition)
            yield return new BakedKeyframe<T>(eventKeyframe.Time, eventKeyframe.Ease, eventKeyframe.Value);
    }

    public static IEnumerable<BakedKeyframe<float>> ResolveRotationKeyframes(KeyframeList<Data.Keyframe<float>> events)
    {
        foreach (var eventKeyframe in events)
            yield return new BakedKeyframe<float>(eventKeyframe.Time, eventKeyframe.Ease, MathUtil.DegreesToRadians(eventKeyframe.Value));
    }

    public static LensDistortionValue LerpLensDistortionData(LensDistortionValue a, LensDistortionValue b, float t)
        => new()
        {
            Intensity = float.Lerp(a.Intensity, b.Intensity, t),
            Center = Vector2.Lerp(a.Center, b.Center, t)
        };

    public static GrainValue LerpGrainData(GrainValue a, GrainValue b, float t)
        => new()
        {
            Intensity = float.Lerp(a.Intensity, b.Intensity, t),
            Size = float.Lerp(a.Size, b.Size, t),
            Mix = float.Lerp(a.Mix, b.Mix, t),
            IsColored = a.IsColored // zero order hold, can't lerp booleans
        };

    public static BloomEffectState ResolveBloomData(BloomValue bloomData, ThemeColorState tcs)
    {
        return new BloomEffectState
        {
            Intensity = bloomData.Intensity,
            Diffusion = bloomData.Diffusion,
            Color = bloomData.Color >= 0 && bloomData.Color < tcs.Effect.Length 
                ? tcs.Effect[bloomData.Color] 
                : new ColorRgb(1f, 1f, 1f)
        };
    }
    
    public static VignetteEffectState ResolveVignetteData(VignetteValue vignetteData, ThemeColorState tcs)
        => new()
        {
            Intensity = vignetteData.Intensity,
            Smoothness = vignetteData.Smoothness,
            Color = vignetteData.Color >= 0 && vignetteData.Color < tcs.Effect.Length
                ? tcs.Effect[vignetteData.Color]
                : default,
            Rounded = vignetteData.IsRounded,
            Roundness = vignetteData.Roundness,
            Center = vignetteData.Center
        };

    public static GradientEffectState ResolveGradientData(GradientValue gradientData, ThemeColorState tcs)
    {
        var colorAIndex = Math.Clamp(gradientData.StartColor, 0, tcs.Effect.Length - 1);
        var colorBIndex = Math.Clamp(gradientData.EndColor, 0, tcs.Effect.Length - 1);
        return new GradientEffectState
        {
            Color1 = tcs.Effect[colorAIndex],
            Color2 = tcs.Effect[colorBIndex],
            Intensity = gradientData.Intensity,
            Rotation = gradientData.Rotation,
            Mode = gradientData.Mode
        };
    }

    public static GlitchValue LerpGlitchData(GlitchValue a, GlitchValue b, float t)
        => new()
        {
            Intensity = float.Lerp(a.Intensity, b.Intensity, t),
            Speed = float.Lerp(a.Speed, b.Speed, t),
            Width = float.Lerp(a.Width, b.Width, t)
        };
}