using System.Numerics;
using TmpParser;

namespace ParallelAnimationSystem.Core.Text;

public static class TextShaperExtension
{
    public static ShapedRichText ShapeText(this TextShaper textShaper, string text, Vector2 origin)
        => textShaper.ShapeText(
            text,
            "NotoMono SDF",
            origin.X switch
            {
                -0.5f => HorizontalAlignment.Right,
                0.5f => HorizontalAlignment.Left,
                _ => HorizontalAlignment.Center,
            },
            origin.Y switch
            {
                -0.5f => VerticalAlignment.Top,
                0.5f => VerticalAlignment.Bottom,
                _ => VerticalAlignment.Center,
            });
}