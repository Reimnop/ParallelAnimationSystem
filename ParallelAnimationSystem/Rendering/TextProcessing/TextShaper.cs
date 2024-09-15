using OpenTK.Mathematics;
using TmpParser;

namespace ParallelAnimationSystem.Rendering.TextProcessing;

public static class TextShaper
{
    private class ShapingState
    {
        public Style CurrentStyle { get; set; }
        public Measurement CurrentCSpace { get; set; }
        public Measurement CurrentSize { get; set; } = new(1.0f, Unit.Em);
        public HorizontalAlignment? CurrentAlignment { get; set; }
    }
    
    public static IEnumerable<RenderGlyph> ShapeText(FontData fontData, string str)
    {
        var tokens = TagParser.EnumerateTokens(str);
        var elements = TagParser.EnumerateElements(tokens);
        var lines = TagParser.EnumerateLines(elements);

        var state = new ShapingState();
        
        var linesOfShapedGlyphs = lines
            .Select(line => ShapeLinePartial(fontData, line, state))
            .ToList();
        
        if (linesOfShapedGlyphs.Count == 0)
            yield break;
        
        // Offset lines according to alignment
        foreach (var line in linesOfShapedGlyphs)
        {
            var alignment = line.Alignment ?? HorizontalAlignment.Center;
            var xOffset = alignment switch
            {
                HorizontalAlignment.Left => 0.0f,
                HorizontalAlignment.Center => -line.Width / 2.0f,
                HorizontalAlignment.Right => -line.Width,
                _ => throw new ArgumentOutOfRangeException()
            };
            var glyphs = line.Glyphs;
            for (var i = 0; i < glyphs.Length; i++)
                glyphs[i] = glyphs[i] with { Position = glyphs[i].Position + xOffset };
        }
        
        // Output render glyphs
        var glyphSizeMultiplier = fontData.Size / fontData.FontFile.Metadata.Size;
        var y = linesOfShapedGlyphs.Sum(line => line.Height) / 2.0f - linesOfShapedGlyphs[0].Ascender;
        foreach (var line in linesOfShapedGlyphs)
        {
            foreach (var shapedGlyph in line.Glyphs)
            {
                var bold = shapedGlyph.Style.Bold;
                var italic = shapedGlyph.Style.Italic;
                var boldItalic = (bold ? BoldItalic.Bold : BoldItalic.None) | (italic ? BoldItalic.Italic : BoldItalic.None);
                
                var colorAlpha = shapedGlyph.Style.Color;
                var color = new Vector4(
                    colorAlpha.Rgb.HasValue 
                        ? new Vector3(
                            colorAlpha.Rgb.Value.R / 255.0f,
                            colorAlpha.Rgb.Value.G / 255.0f,
                            colorAlpha.Rgb.Value.B / 255.0f)
                        : new Vector3(float.NaN), 
                    colorAlpha.A.HasValue
                        ? colorAlpha.A.Value / 255.0f
                        : float.NaN);
                
                var x = shapedGlyph.Position;
                var glyph = fontData.GlyphIdToGlyph[shapedGlyph.GlyphId];
                if (glyph.Width != 0.0f && glyph.Height != 0.0f)
                {
                    var minX = x + glyph.BearingX * shapedGlyph.Size * glyphSizeMultiplier;
                    var minY = y + glyph.BearingY * shapedGlyph.Size * glyphSizeMultiplier;
                    var maxX = minX + glyph.Width * shapedGlyph.Size * glyphSizeMultiplier;
                    var maxY = minY - glyph.Height * shapedGlyph.Size * glyphSizeMultiplier;
                    var minUV = new Vector2(glyph.MinX, glyph.MinY);
                    var maxUV = new Vector2(glyph.MaxX, glyph.MaxY);
                    yield return new RenderGlyph(new Vector2(minX, minY), new Vector2(maxX, maxY), minUV, maxUV, color, boldItalic);
                }
            }
            
            y -= line.Height;
        }
    }

    // Shape a line of text, without X offset (that'll be done in a later phase)
    private static TmpLine ShapeLinePartial(FontData fontData, IEnumerable<IElement> line, ShapingState state)
    {
        var glyphSizeMultiplier = fontData.Size / fontData.FontFile.Metadata.Size;
        
        var glyphs = new List<ShapedGlyph>();
        var x = 0.0f;
        var width = 0.0f;
        var height = 0.0f;
        var ascender = 0.0f;
        var descender = 0.0f;
        foreach (var element in line)
        {
            if (element is TextElement textElement)
            {
                foreach (var c in textElement.Value)
                {
                    var glyphId = GetGlyph(fontData, c);
                    if (!glyphId.HasValue)
                        continue;
                    
                    var currentSize = ResolveMeasurement(state.CurrentSize, fontData.Size, fontData.Size) / fontData.Size;
                    var currentCSpace = ResolveMeasurement(state.CurrentCSpace, fontData.Size, fontData.Size);
                    
                    var glyph = fontData.GlyphIdToGlyph[glyphId.Value];
                    var shapedGlyph = new ShapedGlyph(x, glyphId.Value, currentSize, state.CurrentStyle);
                    glyphs.Add(shapedGlyph);
                    x += glyph.Advance * currentSize * glyphSizeMultiplier + currentCSpace * currentSize;
                    
                    width = Math.Max(width, shapedGlyph.Position + glyph.Advance * currentSize * glyphSizeMultiplier);
                    height = Math.Max(height, fontData.FontFile.Metadata.LineHeight * currentSize * glyphSizeMultiplier);
                    ascender = Math.Max(ascender, fontData.FontFile.Metadata.Ascender * currentSize * glyphSizeMultiplier);
                    descender = Math.Min(descender, fontData.FontFile.Metadata.Descender * currentSize * glyphSizeMultiplier);
                }
            }
            
            if (element is StyleElement styleElement)
            {
                var currentStyle = state.CurrentStyle;
                if (styleElement.Bold.HasValue)
                    currentStyle = currentStyle with { Bold = styleElement.Bold.Value };
                if (styleElement.Italic.HasValue)
                    currentStyle = currentStyle with { Italic = styleElement.Italic.Value };
                if (styleElement.Underline.HasValue)
                    currentStyle = currentStyle with { Underline = styleElement.Underline.Value };
                if (styleElement.Color.HasValue)
                    currentStyle = currentStyle with { Color = styleElement.Color.Value };
                state.CurrentStyle = currentStyle;
            }
            
            if (element is AlignElement alignElement)
            {
                state.CurrentAlignment = alignElement.Alignment;
            }

            if (element is CSpaceElement cSpaceElement)
            {
                state.CurrentCSpace = cSpaceElement.Value;
            }

            if (element is PosElement posElement)
            {
                x += ResolveMeasurement(posElement.Value, fontData.Size, 0.0f);
            }

            if (element is SizeElement sizeElement)
            {
                state.CurrentSize = sizeElement.Value;
            }
        }
        
        var lastCurrentSize = ResolveMeasurement(state.CurrentSize, fontData.Size, fontData.Size);
        height = height == 0.0f ? fontData.FontFile.Metadata.LineHeight * lastCurrentSize * glyphSizeMultiplier : height;
        ascender = ascender == 0.0f ? fontData.FontFile.Metadata.Ascender * lastCurrentSize * glyphSizeMultiplier : ascender;
        descender = descender == 0.0f ? fontData.FontFile.Metadata.Descender * lastCurrentSize * glyphSizeMultiplier : descender;
        
        return new TmpLine(ascender, descender, width, height, state.CurrentAlignment, glyphs.ToArray());
    }

    private static float ResolveMeasurement(Measurement measurement, float baseEm, float basePercent)
        => measurement.Unit switch
        {
            Unit.Pixel => measurement.Value,
            Unit.Em => measurement.Value * baseEm,
            Unit.Percent => measurement.Value * basePercent,
            _ => throw new ArgumentOutOfRangeException(nameof(measurement)),
        };

    private static int? GetGlyph(FontData fontData, char c)
    {
        if (!fontData.CharacterToGlyphId.TryGetValue(c, out var glyphId))
            return null;
        return glyphId;
    }
}