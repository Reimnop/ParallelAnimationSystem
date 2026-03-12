using System.Numerics;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Rendering.Data;

public struct HueShiftEffectState
{
    public float Angle;
}

public struct BloomEffectState
{
    public float Intensity;
    public float Diffusion;
    public ColorRgb Color;
}

public struct LensDistortionEffectState
{
    public float Intensity;
    public Vector2 Center;
}

public struct ChromaticAberrationEffectState
{
    public float Intensity;
}

public struct VignetteEffectState
{
    public Vector2 Center;
    public float Intensity;
    public bool Rounded;
    public float Roundness;
    public float Smoothness;
    public ColorRgb Color;
    public VignetteMode Mode;
}

public struct GradientEffectState
{
    public ColorRgb Color1;
    public ColorRgb Color2;
    public float Intensity;
    public float Rotation;
    public GradientOverlayMode Mode;
}

public struct GlitchEffectState
{
    public float Speed;
    public float Intensity;
    public float Amount;
    public float StretchMultiplier;
}

public struct PostProcessingState
{
    public float Time;
    public HueShiftEffectState HueShift;
    public BloomEffectState LegacyBloom;
    public BloomEffectState UniversalBloom;
    public LensDistortionEffectState LensDistortion;
    public ChromaticAberrationEffectState ChromaticAberration;
    public VignetteEffectState Vignette;
    public GradientEffectState Gradient;
    public GlitchEffectState Glitch;
}