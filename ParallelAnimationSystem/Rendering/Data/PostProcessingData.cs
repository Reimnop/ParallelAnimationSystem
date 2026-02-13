using System.Numerics;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Rendering.Data;

public record struct HueShiftPostProcessingData(float Angle);
public record struct BloomPostProcessingData(float Intensity, float Diffusion);
public record struct LensDistortionPostProcessingData(float Intensity, Vector2 Center);
public record struct ChromaticAberrationPostProcessingData(float Intensity);
public record struct VignettePostProcessingData(Vector2 Center, float Intensity, bool Rounded, float Roundness, float Smoothness, ColorRgb Color);
public record struct GradientPostProcessingData(ColorRgb Color1, ColorRgb Color2, float Intensity, float Rotation, GradientOverlayMode Mode);
public record struct GlitchPostProcessingData(float Intensity, float Speed, Vector2 Size);

public record struct PostProcessingData(
    float Time,
    HueShiftPostProcessingData HueShift,
    BloomPostProcessingData LegacyBloom,
    BloomPostProcessingData UniversalBloom,
    LensDistortionPostProcessingData LensDistortion,
    ChromaticAberrationPostProcessingData ChromaticAberration,
    VignettePostProcessingData Vignette,
    GradientPostProcessingData Gradient,
    GlitchPostProcessingData Glitch);