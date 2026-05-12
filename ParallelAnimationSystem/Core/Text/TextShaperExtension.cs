using System.Numerics;
using Tmpx.Shaping;

namespace ParallelAnimationSystem.Core.Text;

public static class TextShaperExtension
{
    public static ShapedRichText ShapeText(this TextShaper textShaper, string text, Vector2 origin)
        => textShaper.ShapeText(
            text,
            "NotoSans",
            origin.X switch
            {
                -0.5f => TextHorizontalAlignment.Right,
                0.5f => TextHorizontalAlignment.Left,
                _ => TextHorizontalAlignment.Center,
            },
            origin.Y switch
            {
                -0.5f => TextVerticalAlignment.Top,
                0.5f => TextVerticalAlignment.Bottom,
                _ => TextVerticalAlignment.Middle,
            });
}