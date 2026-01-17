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
}