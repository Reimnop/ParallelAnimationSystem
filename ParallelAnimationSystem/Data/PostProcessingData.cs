using OpenTK.Mathematics;
using Pamx.Common.Enum;

namespace ParallelAnimationSystem.Data;

public record struct PostProcessingData(
    float BloomIntensity, float BloomDiffusion,
    float HueShiftAngle,
    float LensDistortionIntensity, Vector2 LensDistortionCenter,
    float ChromaticAberrationIntensity,
    Vector2 VignetteCenter, float VignetteIntensity, bool VignetteRounded, float VignetteRoundness, float VignetteSmoothness, Vector3 VignetteColor,
    Vector3 GradientColor1, Vector3 GradientColor2, float GradientIntensity, float GradientRotation, GradientOverlayMode GradientMode);