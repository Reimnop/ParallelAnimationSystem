using System.Numerics;
using Pamx.Common.Data;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Core.Animation;

public static class EventHelper
{
    public static IEnumerable<Keyframe<T>> ResolveGenericKeyframes<T>(KeyframeList<EventKeyframe<T>> eventsCameraPosition)
    {
        foreach (var eventKeyframe in eventsCameraPosition)
            yield return new Keyframe<T>(eventKeyframe.Time, eventKeyframe.Ease, eventKeyframe.Value);
    }

    public static IEnumerable<Keyframe<float>> ResolveRotationKeyframes(KeyframeList<EventKeyframe<float>> events)
    {
        foreach (var eventKeyframe in events)
            yield return new Keyframe<float>(eventKeyframe.Time, eventKeyframe.Ease, MathUtil.DegreesToRadians(eventKeyframe.Value));
    }

    public static LensDistortionData LerpLensDistortionData(LensDistortionData a, LensDistortionData b, float t)
        => new()
        {
            Intensity = float.Lerp(a.Intensity, b.Intensity, t),
            Center = Vector2.Lerp(a.Center, b.Center, t)
        };

    public static GrainData LerpGrainData(GrainData a, GrainData b, float t)
        => new()
        {
            Intensity = float.Lerp(a.Intensity, b.Intensity, t),
            Size = float.Lerp(a.Size, b.Size, t),
            Mix = float.Lerp(a.Mix, b.Mix, t),
            Colored = a.Colored // zero order hold, can't lerp booleans
        };

    public static BloomEffectState ResolveBloomData(BloomData bloomData, ThemeColorState tcs)
    {
        var colorIndex = Math.Clamp(bloomData.Color, 0, tcs.Effect.Length - 1);
        return new BloomEffectState
        {
            Intensity = bloomData.Intensity,
            Diffusion = bloomData.Diffusion,
            Color = tcs.Effect[colorIndex]
        };
    }
    
    public static VignetteEffectState ResolveVignetteData(VignetteData vignetteData, ThemeColorState tcs)
        => new()
        {
            Intensity = vignetteData.Intensity,
            Smoothness = vignetteData.Smoothness,
            Color = vignetteData.Color.HasValue 
                ? tcs.Effect[Math.Clamp(vignetteData.Color.Value, 0, tcs.Effect.Length - 1)]
                : default,
            Rounded = vignetteData.Rounded,
            Roundness = vignetteData.Roundness ?? 1f,
            Center = vignetteData.Center
        };

    public static GradientEffectState ResolveGradientData(GradientData gradientData, ThemeColorState tcs)
    {
        var colorAIndex = Math.Clamp(gradientData.ColorA, 0, tcs.Effect.Length - 1);
        var colorBIndex = Math.Clamp(gradientData.ColorB, 0, tcs.Effect.Length - 1);
        return new GradientEffectState
        {
            Color1 = tcs.Effect[colorAIndex],
            Color2 = tcs.Effect[colorBIndex],
            Intensity = gradientData.Intensity,
            Rotation = gradientData.Rotation,
            Mode = gradientData.Mode
        };
    }

    public static GlitchData LerpGlitchData(GlitchData a, GlitchData b, float t)
        => new()
        {
            Intensity = float.Lerp(a.Intensity, b.Intensity, t),
            Speed = float.Lerp(a.Speed, b.Speed, t),
            Width = float.Lerp(a.Width, b.Width, t)
        };
}