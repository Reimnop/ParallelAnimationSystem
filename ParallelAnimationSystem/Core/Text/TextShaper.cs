using System.Numerics;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Service;
using Tmpx.Common;
using Tmpx.Parsing;
using Tmpx.Shaping;

namespace ParallelAnimationSystem.Core.Text;

public class TextShaper(FontService fontService)
{
    private const float WorldUnitsPerEm = 1.5f;

    public ShapedRichText ShapeText(
        string text,
        string defaultFontName,
        TextHorizontalAlignment horizontalAlignment,
        TextVerticalAlignment verticalAlignment)
    {
        var output = new ShapedRichText();
        ShapeText(text, defaultFontName, horizontalAlignment, verticalAlignment, output);
        return output;
    }

    public void ShapeText(
        string text,
        string defaultFontName,
        TextHorizontalAlignment horizontalAlignment,
        TextVerticalAlignment verticalAlignment,
        ShapedRichText output)
    {
        output.Glyphs.Clear();

        var tokens = TextParser.Parse(text);
        var emToWorld = Matrix3x2.CreateScale(WorldUnitsPerEm);

        fontService.Shaper.Shape(tokens, horizontalAlignment, verticalAlignment, defaultFontName,
            (transform, color, font, localShapeEntryIndex) =>
            {
                var globalShapeEntryIndex = localShapeEntryIndex;
                if (font is not null && localShapeEntryIndex >= 0)
                {
                    var base_ = fontService.GetGlobalShapeEntryBase(font);
                    globalShapeEntryIndex = base_ + localShapeEntryIndex;
                }

                output.Glyphs.Add(new ShapedTextGlyph(
                    transform * emToWorld,
                    ToColorRgba(color),
                    globalShapeEntryIndex));
            });
    }

    private static ColorRgba ToColorRgba(Color c)
        => new(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
}
