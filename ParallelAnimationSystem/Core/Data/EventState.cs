using System.Numerics;
using Pamx.Events;

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
    public LensDistortionValue LensDistortion { get; set; }
    public GrainValue Grain { get; set; }
    public GradientEffectState Gradient { get; set; }
    public GlitchValue Glitch { get; set; }
    public float Hue { get; set; }
}