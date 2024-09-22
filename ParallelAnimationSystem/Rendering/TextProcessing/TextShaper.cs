using OpenTK.Mathematics;
using ParallelAnimationSystem.Util;
using TmpParser;

namespace ParallelAnimationSystem.Rendering.TextProcessing;

public class TextShaper(List<FontData> registeredFonts)
{
    private class ShapingState
    {
        public Style CurrentStyle { get; set; }
        public Measurement CurrentCSpace { get; set; }
        public Measurement CurrentSize { get; set; } = new(1.0f, Unit.Em);
        public HorizontalAlignment? CurrentAlignment { get; set; }
    }
    
    public IEnumerable<RenderGlyph> ShapeText(FontStack fontStack, string str)
    {
        var tokens = TagParser.EnumerateTokens(str);
        var elements = TagParser.EnumerateElements(tokens);
        var lines = TagParser.EnumerateLines(elements);

        var state = new ShapingState();
        
        var linesOfShapedGlyphs = lines
            .Select(line => ShapeLinePartial(fontStack, line, state))
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
                var font = registeredFonts[shapedGlyph.FontIndex];
                var glyph = font.GlyphIdToGlyph[shapedGlyph.GlyphId];
                if (glyph.Width != 0.0f && glyph.Height != 0.0f)
                {
                    var minX = x + glyph.BearingX / font.Metadata.Size * shapedGlyph.Size;
                    var minY = y + glyph.BearingY / font.Metadata.Size * shapedGlyph.Size;
                    var maxX = minX + glyph.Width / font.Metadata.Size * shapedGlyph.Size;
                    var maxY = minY - glyph.Height / font.Metadata.Size * shapedGlyph.Size;
                    var minUV = new Vector2(glyph.MinX, glyph.MinY);
                    var maxUV = new Vector2(glyph.MaxX, glyph.MaxY);
                    yield return new RenderGlyph(new Vector2(minX, minY), new Vector2(maxX, maxY), minUV, maxUV, color, boldItalic, shapedGlyph.FontIndex);
                }
            }
            
            y -= line.Height;
        }
    }

    // Shape a line of text, without X offset (that'll be done in a later phase)
    private TmpLine ShapeLinePartial(FontStack fontStack, IEnumerable<IElement> line, ShapingState state)
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
                    var rawGlyph = GetGlyph(fontStack, c);
                    if (!rawGlyph.HasValue)
                        continue;
                    
                    var (glyphId, fontHandleIndex) = rawGlyph.Value;
                    var fontHandle = fontStack.Fonts[fontHandleIndex];
                    var font = registeredFonts[fontHandle.Index];
                    
                    var currentSize = ResolveMeasurement(state.CurrentSize, fontStack.Size, fontStack.Size);
                    var currentCSpace = ResolveMeasurement(state.CurrentCSpace, fontStack.Size, fontStack.Size);
                    
                    var glyph = font.GlyphIdToGlyph[glyphId];
                    var shapedGlyph = new ShapedGlyph(x, fontHandle.Index, glyphId, currentSize, state.CurrentStyle);
                    glyphs.Add(shapedGlyph);
                    
                    // Calculate advance
                    var normalizedAdvance = glyph.Advance / font.Metadata.Size;
                    x += normalizedAdvance * currentSize + currentCSpace;
                    
                    var normalizedLineHeight = font.Metadata.LineHeight / font.Metadata.Size;
                    var normalizedAscender = font.Metadata.Ascender / font.Metadata.Size;
                    var normalizedDescender = font.Metadata.Descender / font.Metadata.Size;
                    
                    width = Math.Max(width, shapedGlyph.Position + normalizedAdvance * currentSize);
                    height = Math.Max(height, normalizedLineHeight * currentSize);
                    ascender = Math.Max(ascender, normalizedAscender * currentSize);
                    descender = Math.Min(descender, normalizedDescender * currentSize);
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
                x += ResolveMeasurement(posElement.Value, fontStack.Size, 0.0f);
            }

            if (element is SizeElement sizeElement)
            {
                state.CurrentSize = sizeElement.Value;
            }
        }
        
        var lastCurrentSize = ResolveMeasurement(state.CurrentSize, fontStack.Size, fontStack.Size);
        var firstFontHandle = fontStack.Fonts[0];
        var firstFont = registeredFonts[firstFontHandle.Index];
        var normalizedLastLineHeight = firstFont.Metadata.LineHeight / firstFont.Metadata.Size;
        var normalizedLastAscender = firstFont.Metadata.Ascender / firstFont.Metadata.Size;
        var normalizedLastDescender = firstFont.Metadata.Descender / firstFont.Metadata.Size;
        
        height = height == 0.0f ? normalizedLastLineHeight * lastCurrentSize : height;
        ascender = ascender == 0.0f ? normalizedLastAscender * lastCurrentSize : ascender;
        descender = descender == 0.0f ? normalizedLastDescender * lastCurrentSize : descender;
        
        return new TmpLine(ascender, descender, width, height, state.CurrentAlignment, glyphs.ToArray());
    }

    private float ResolveMeasurement(Measurement measurement, float baseEm, float basePercent)
        => measurement.Unit switch
        {
            Unit.Pixel => measurement.Value,
            Unit.Em => measurement.Value * baseEm,
            Unit.Percent => measurement.Value * basePercent,
            _ => throw new ArgumentOutOfRangeException(nameof(measurement)),
        };

    private (int GlyphId, int FontHandleIndex)? GetGlyph(FontStack fontStack, char c)
    {
        foreach (var (i, fontHandle) in fontStack.Fonts.Indexed())
            if (registeredFonts[fontHandle.Index].CharacterToGlyphId.TryGetValue(c, out var glyphId))
                return (glyphId, i);
        return null;
    }
}