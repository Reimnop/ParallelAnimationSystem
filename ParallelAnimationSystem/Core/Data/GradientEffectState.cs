using System.Numerics;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Animation;

namespace ParallelAnimationSystem.Core.Data;

public struct GradientEffectState()
{
    public Vector3 Color1 { get; set; } = Vector3.Zero;
    public Vector3 Color2 { get; set; } = Vector3.Zero;
    public float Intensity { get; set; } = 1.0f;
    public float Rotation { get; set; } = 0.0f;
    public GradientOverlayMode Mode { get; set; } = GradientOverlayMode.Linear;

    public static GradientEffectState Lerp(GradientEffectState a, GradientEffectState b, float t)
        => new()
        {
            Color1 = Vector3.Lerp(a.Color1, b.Color1, t),
            Color2 = Vector3.Lerp(a.Color2, b.Color2, t),
            Intensity = float.Lerp(a.Intensity, b.Intensity, t),
            Rotation = float.Lerp(a.Rotation, b.Rotation, t),
            Mode = b.Mode // zero order hold, can't lerp enums
        };
}