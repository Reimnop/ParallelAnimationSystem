using System.Numerics;

namespace ParallelAnimationSystem.Core.Data;

public struct VignetteEffectState()
{
    public float Intensity { get; set; } = 0.0f;
    public float Smoothness { get; set; } = 0.0f;
    public Vector3 Color { get; set; } = Vector3.Zero;
    public bool Rounded { get; set; } = false;
    public float Roundness { get; set; } = 0.0f;
    public Vector2 Center { get; set; } = Vector2.Zero;
    
    public static VignetteEffectState Lerp(VignetteEffectState a, VignetteEffectState b, float t)
        => new()
        {
            Intensity = float.Lerp(a.Intensity, b.Intensity, t),
            Smoothness = float.Lerp(a.Smoothness, b.Smoothness, t),
            Color = Vector3.Lerp(a.Color, b.Color, t),
            Rounded = a.Rounded, // zero order hold, can't lerp booleans
            Roundness = float.Lerp(a.Roundness, b.Roundness, t),
            Center = Vector2.Lerp(a.Center, b.Center, t)
        };
}