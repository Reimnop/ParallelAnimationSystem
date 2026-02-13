namespace ParallelAnimationSystem.Core.Data;

public struct BloomEffectState()
{
    public float Intensity { get; set; } = 0.0f;
    public float Diffusion { get; set; } = 0.0f;
    public ColorRgb Color { get; set; } = new(1f, 1f, 1f);
    
    public static BloomEffectState Lerp(BloomEffectState from, BloomEffectState to, float t)
        => new()
        {
            Intensity = float.Lerp(from.Intensity, to.Intensity, t),
            Diffusion = float.Lerp(from.Diffusion, to.Diffusion, t),
            Color = ColorRgb.Lerp(from.Color, to.Color, t)
        };
}