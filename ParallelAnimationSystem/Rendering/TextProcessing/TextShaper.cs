using OpenTK.Mathematics;
using TmpParser;

namespace ParallelAnimationSystem.Rendering.TextProcessing;

public static class TextShaper
{
    private class ShapingState
    {
        public Style CurrentStyle { get; set; }
        public float CurrentCSpace { get; set; }
        public float CurrentSize { get; set; }
        public HorizontalAlignment? CurrentAlignment { get; set; }
    }
    
    public static IEnumerable<RenderGlyph> ShapeText(FontData fontData, string str)
    {
        var tokens = TagParser.EnumerateTokens(str);
        var elements = TagParser.EnumerateElements(tokens);
        var lines = TagParser.EnumerateLines(elements);

        var state = new ShapingState
        {
            CurrentSize = 1.0f,
        };
        
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
                    var minX = (x + glyph.BearingX * shapedGlyph.Size) / fontData.FontFile.Metadata.Size;
                    var minY = (y + glyph.BearingY * shapedGlyph.Size) / fontData.FontFile.Metadata.Size;
                    var maxX = minX + glyph.Width * shapedGlyph.Size / fontData.FontFile.Metadata.Size;
                    var maxY = minY - glyph.Height * shapedGlyph.Size / fontData.FontFile.Metadata.Size;
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
                    var glyph = fontData.GlyphIdToGlyph[glyphId.Value];
                    var shapedGlyph = new ShapedGlyph(x, glyphId.Value, state.CurrentSize, state.CurrentStyle);
                    glyphs.Add(shapedGlyph);
                    x += (glyph.Advance + state.CurrentCSpace) * state.CurrentSize;
                    
                    width = Math.Max(width, x);
                    height = Math.Max(height, fontData.FontFile.Metadata.LineHeight * state.CurrentSize);
                    ascender = Math.Max(ascender, fontData.FontFile.Metadata.Ascender * state.CurrentSize);
                    descender = Math.Min(descender, fontData.FontFile.Metadata.Descender * state.CurrentSize);
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
                state.CurrentCSpace = cSpaceElement.CSpace;
            }

            if (element is PosElement posElement)
            {
                x += posElement.Value;
            }

            if (element is SizeElement sizeElement)
            {
                state.CurrentSize = sizeElement.Value / 16.0f; // TODO: Don't hardcode this
            }
        }
        
        height = height == 0.0f ? fontData.FontFile.Metadata.LineHeight * state.CurrentSize : height;
        ascender = ascender == 0.0f ? fontData.FontFile.Metadata.Ascender * state.CurrentSize : ascender;
        descender = descender == 0.0f ? fontData.FontFile.Metadata.Descender * state.CurrentSize : descender;
        
        return new TmpLine(ascender, descender, width, height, state.CurrentAlignment, glyphs.ToArray());
    }

    private static int? GetGlyph(FontData fontData, char c)
    {
        if (!fontData.CharacterToGlyphId.TryGetValue(c, out var glyphId))
            return null;
        return glyphId;
    }
}