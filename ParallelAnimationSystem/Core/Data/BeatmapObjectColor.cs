namespace ParallelAnimationSystem.Core.Data;

public struct BeatmapObjectColor(ColorRgb color1, ColorRgb color2, float opacity)
{
    public ColorRgb Color1 => color1;
    public ColorRgb Color2 => color2;
    public float Opacity => opacity;
    
    public static BeatmapObjectColor Lerp(BeatmapObjectColor a, BeatmapObjectColor b, float t)
        => new(
            ColorRgb.Lerp(a.Color1, b.Color1, t),
            ColorRgb.Lerp(a.Color2, b.Color2, t),
            float.Lerp(a.Opacity, b.Opacity, t));

    public static BeatmapObjectColor Resolve(BeatmapObjectIndexedColor color, ThemeColorState context)
        => new(
            context.Object[color.ColorIndex1], 
            context.Object[color.ColorIndex2], 
            color.Opacity);
}