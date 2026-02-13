using System.Numerics;
using Pamx.Common.Enum;

namespace ParallelAnimationSystem.Core.Data;

public struct GradientEffectState()
{
    public ColorRgb Color1 { get; set; } = default;
    public ColorRgb Color2 { get; set; } = default;
    public float Intensity { get; set; } = 1.0f;
    public float Rotation { get; set; } = 0.0f;
    public GradientOverlayMode Mode { get; set; } = GradientOverlayMode.Linear;

    public static GradientEffectState Lerp(GradientEffectState a, GradientEffectState b, float t)
        => new()
        {
            Color1 = ColorRgb.Lerp(a.Color1, b.Color1, t),
            Color2 = ColorRgb.Lerp(a.Color2, b.Color2, t),
            Intensity = float.Lerp(a.Intensity, b.Intensity, t),
            Rotation = float.Lerp(a.Rotation, b.Rotation, t),
            Mode = b.Mode // zero order hold, can't lerp enums
        };
}