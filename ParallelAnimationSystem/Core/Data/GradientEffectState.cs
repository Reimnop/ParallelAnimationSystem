using System.Numerics;
using Pamx.Common.Enum;

namespace ParallelAnimationSystem.Core.Data;

public struct GradientEffectState()
{
    public Vector3 Color1 { get; set; } = Vector3.Zero;
    public Vector3 Color2 { get; set; } = Vector3.Zero;
    public float Intensity { get; set; } = 1.0f;
    public float Rotation { get; set; } = 0.0f;
    public GradientOverlayMode Mode { get; set; } = GradientOverlayMode.Linear;
}