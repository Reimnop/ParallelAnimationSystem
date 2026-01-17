using System.Numerics;

namespace ParallelAnimationSystem.Core.Data;

public struct BloomEffectState()
{
    public float Intensity { get; set; } = 0.0f;
    public float Diffusion { get; set; } = 0.0f;
    public Vector3 Color { get; set; } = Vector3.One;
}