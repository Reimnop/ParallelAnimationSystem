using System.Numerics;
using Pamx.Common.Enum;

namespace ParallelAnimationSystem.Data;

public record struct PostProcessingData(
    float Time,
    float HueShiftAngle,
    float BloomIntensity, float BloomDiffusion,
    float LensDistortionIntensity, Vector2 LensDistortionCenter,
    float ChromaticAberrationIntensity,
    Vector2 VignetteCenter, float VignetteIntensity, bool VignetteRounded, float VignetteRoundness, float VignetteSmoothness, Vector3 VignetteColor,
    Vector3 GradientColor1, Vector3 GradientColor2, float GradientIntensity, float GradientRotation, GradientOverlayMode GradientMode,
    float GlitchIntensity, float GlitchSpeed, Vector2 GlitchSize);