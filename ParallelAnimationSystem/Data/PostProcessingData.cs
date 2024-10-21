using OpenTK.Mathematics;

namespace ParallelAnimationSystem.Data;

public record struct PostProcessingData(
    float BloomIntensity, float BloomDiffusion,
    float HueShiftAngle,
    float LensDistortionIntensity, Vector2 LensDistortionCenter,
    float ChromaticAberrationIntensity,
    Vector2 VignetteCenter, float VignetteIntensity, float VignetteRoundness, float VignetteSmoothness, Vector3 VignetteColor);