namespace Tmpx.Shaping;

public record GlyphTransform(float ScaleX, float Rotation)
{ 
    public static GlyphTransform Identity => new(1, 0);
}