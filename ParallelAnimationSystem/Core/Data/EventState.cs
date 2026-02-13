using System.Numerics;
using Pamx.Common.Data;

namespace ParallelAnimationSystem.Core.Data;

public class EventState
{
    public Vector2 CameraPosition { get; set; }
    public float CameraScale { get; set; }
    public float CameraRotation { get; set; }
    public float CameraShake { get; set; }
    public float Chroma { get; set; }
    public BloomEffectState Bloom { get; set; }
    public VignetteEffectState Vignette { get; set; }
    public LensDistortionData LensDistortion { get; set; }
    public GrainData Grain { get; set; }
    public GradientEffectState Gradient { get; set; }
    public GlitchData Glitch { get; set; }
    public float Hue { get; set; }
}