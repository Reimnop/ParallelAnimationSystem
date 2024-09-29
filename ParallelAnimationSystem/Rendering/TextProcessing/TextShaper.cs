using OpenTK.Mathematics;
using ParallelAnimationSystem.Util;
using TmpIO;
using TmpParser;

namespace ParallelAnimationSystem.Rendering.TextProcessing;

public delegate T RenderGlyphFactory<out T>(Vector2 min, Vector2 max, Vector2 minUV, Vector2 maxUV, Vector4 color, BoldItalic boldItalic, int fontIndex);

public class TextShaper<T>(
    RenderGlyphFactory<T> renderGlyphFactory,
    Func<IFontHandle, char, TmpCharacter?> getCharacterFromOrdinal,
    Func<IFontHandle, int, TmpGlyph?> getGlyphFromGlyphId,
    Func<IFontHandle, TmpMetadata> getFontMetadata,
    IReadOnlyList<IFontHandle> registeredFonts,
    IReadOnlyList<FontStack> fontStacks,
    string defaultFontName)
{
    private class ShapingState
    {
        public Style CurrentStyle { get; set; }
        public Measurement CurrentCSpace { get; set; }
        public Measurement CurrentSize { get; set; } = new(1.0f, Unit.Em);
        public Measurement CurrentVOffset { get; set; }
        public Measurement CurrentLineHeight { get; set; } = new(1.0f, Unit.Em);
        public HorizontalAlignment? CurrentAlignment { get; set; }
        public ColorAlpha CurrentMarkColor { get; set; }
        public required FontStack CurrentFontStack { get; set; }
    }

    private readonly Dictionary<IFontHandle, int> fontIndexLookup = registeredFonts
        .Indexed()
        .ToDictionary(x => x.Value, x => x.Index);

    private readonly Dictionary<string, FontStack> fontStackLookup = fontStacks
        .ToDictionary(x => x.Name.ToLowerInvariant());
    
    public IEnumerable<T> ShapeText(string str, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment)
    {
        if (str.EndsWith('\n'))
            str = str[..^1]; // ONLY remove the VERY last newline (TMP quirk)
        
        var tokens = TagParser.EnumerateTokens(str);
        var elements = TagParser.EnumerateElements(tokens);
        var lines = TagParser.EnumerateLines(elements)
            .Select(x => x.ToList())
            .ToList();
        
        // Trim the end of each line
        foreach (var line in lines)
        {
            TextElement? lastTextElement = null;
            foreach (var element in line)
                if (element is TextElement textElement)
                    lastTextElement = textElement;
            if (lastTextElement is not null)
                lastTextElement.Value = lastTextElement.Value.TrimEnd();
        }

        var state = new ShapingState
        {
            CurrentFontStack = fontStackLookup[defaultFontName.ToLowerInvariant()],
        };
        
        var linesOfShapedGlyphs = lines
            .Select(line => ShapeLinePartial(line, state))
            .ToList();
        
        if (linesOfShapedGlyphs.Count == 0)
            yield break;
        
        // Offset lines according to alignment
        foreach (var line in linesOfShapedGlyphs)
        {
            var alignment = line.Alignment ?? horizontalAlignment;
            var xOffset = alignment switch
            {
                HorizontalAlignment.Left => 0.0f,
                HorizontalAlignment.Center => -line.Width / 2.0f,
                HorizontalAlignment.Right => -line.Width,
                _ => throw new ArgumentOutOfRangeException(),
            };
            var glyphs = line.Glyphs;
            for (var i = 0; i < glyphs.Length; i++)
                glyphs[i] = glyphs[i] with { Position = glyphs[i].Position + xOffset };
            var marks = line.Marks;
            for (var i = 0; i < marks.Length; i++)
            {
                marks[i] = marks[i] with
                {
                    MinX = marks[i].MinX + xOffset,
                    MaxX = marks[i].MaxX + xOffset,
                };
            }
        }
        
        // Output render glyphs
        var paragraphHeight = GetParagraphHeight(linesOfShapedGlyphs);
        var yOffset = verticalAlignment switch
        {
            VerticalAlignment.Top => paragraphHeight,
            VerticalAlignment.Center => paragraphHeight / 2.0f,
            VerticalAlignment.Bottom => 0.0f,
            _ => throw new ArgumentOutOfRangeException(nameof(verticalAlignment)),
        };

        var y = yOffset - linesOfShapedGlyphs[0].Ascender;
        for (var i = 0; i < linesOfShapedGlyphs.Count; i++)
        {
            var line = linesOfShapedGlyphs[i];
            
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
                var glyphNullable = getGlyphFromGlyphId(font, shapedGlyph.GlyphId);
                var glyph = glyphNullable ?? throw new InvalidOperationException($"Glyph ID '{shapedGlyph.GlyphId}' not found in font");
                if (glyph.Width != 0.0f && glyph.Height != 0.0f)
                {
                    var metadata = getFontMetadata(font);
                    
                    var minX = x + glyph.BearingX / metadata.Size * shapedGlyph.Size;
                    var minY = y + glyph.BearingY / metadata.Size * shapedGlyph.Size + shapedGlyph.YOffset;
                    var maxX = minX + glyph.Width / metadata.Size * shapedGlyph.Size;
                    var maxY = minY - glyph.Height / metadata.Size * shapedGlyph.Size;
                    var minUV = new Vector2(glyph.MinX, glyph.MinY);
                    var maxUV = new Vector2(glyph.MaxX, glyph.MaxY);
                    yield return renderGlyphFactory(new Vector2(minX, minY), new Vector2(maxX, maxY), minUV, maxUV, color, boldItalic, shapedGlyph.FontIndex);
                }
            }

            foreach (var mark in line.Marks)
            {
                var colorAlpha = mark.Color;
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
                
                // Discard marks with 0 alpha since we can't see them anyway
                if (color.W == 0.0f)
                    continue;
                
                var minX = mark.MinX;
                var minY = y + mark.MinY;
                var maxX = mark.MaxX;
                var maxY = y + mark.MaxY;
                yield return renderGlyphFactory(new Vector2(minX, minY), new Vector2(maxX, maxY), Vector2.Zero, Vector2.Zero, color, BoldItalic.None, -1);
            }
            
            if (i + 1 < linesOfShapedGlyphs.Count)
                y -= linesOfShapedGlyphs[i + 1].Height;
        }
    }

    // Shape a line of text, without X offset (that'll be done in a later phase)
    private TmpLine ShapeLinePartial(IEnumerable<IElement> line, ShapingState state)
    {
        var initialHasMark = state.CurrentMarkColor.Rgb.HasValue;
        var glyphs = new List<ShapedGlyph>();
        var marks = new List<Mark>();
        Mark? currentMark = initialHasMark
            ? new Mark(0.0f, 0.0f, 0.0f, 0.0f, state.CurrentMarkColor)
            : null;
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
                    var fontStack = state.CurrentFontStack;
                    var rawGlyph = GetGlyph(fontStack, c);
                    if (!rawGlyph.HasValue)
                        continue;
                    
                    var (glyphId, fontHandleIndex) = rawGlyph.Value;
                    var fontHandle = fontStack.Fonts[fontHandleIndex];
                    var fontIndex = fontIndexLookup[fontHandle];
                    var font = registeredFonts[fontIndex];
                    var fontMetadata = getFontMetadata(font);
                    
                    var currentSize = ResolveMeasurement(state.CurrentSize, fontStack.Size, fontStack.Size);
                    var currentCSpace = ResolveMeasurement(state.CurrentCSpace, fontStack.Size, fontStack.Size);
                    var currentVOffset = ResolveMeasurement(
                        state.CurrentVOffset,
                        fontMetadata.LineHeight / fontMetadata.Size * currentSize,
                        fontMetadata.LineHeight / fontMetadata.Size * currentSize);
                    
                    var glyphNullable = getGlyphFromGlyphId(font, glyphId);
                    var glyph = glyphNullable ?? throw new InvalidOperationException($"Glyph ID '{glyphId}' not found in font");
                    
                    var glyphPosition = x;
                    var shapedGlyph = new ShapedGlyph(glyphPosition, currentVOffset, fontIndex, glyphId, currentSize, state.CurrentStyle);
                    glyphs.Add(shapedGlyph);
                    
                    // Calculate advance
                    var metadata = getFontMetadata(font);
                    var normalizedAdvance = glyph.Advance / metadata.Size;
                    x += normalizedAdvance * currentSize + currentCSpace;
                    
                    var normalizedAscender = metadata.Ascender / metadata.Size;
                    var normalizedDescender = metadata.Descender / metadata.Size;
                    
                    var glyphEnd = glyphPosition + normalizedAdvance * currentSize;
                    var glyphUpper = Math.Max(normalizedAscender * currentSize, normalizedAscender * currentSize + currentVOffset);
                    var glyphLower = Math.Min(normalizedDescender * currentSize, normalizedDescender * currentSize + currentVOffset);
                    var glyphHeight = glyphUpper - glyphLower;
                    
                    width = Math.Max(width, glyphEnd);
                    height = Math.Max(height, glyphHeight);
                    ascender = Math.Max(ascender, glyphUpper);
                    descender = Math.Min(descender, glyphLower);

                    if (currentMark.HasValue)
                    {
                        var currentMaxX = currentMark.Value.MaxX;
                        var currentMinY = currentMark.Value.MinY;
                        var currentMaxY = currentMark.Value.MaxY;
                        currentMark = currentMark.Value with
                        {
                            MaxX = Math.Max(currentMaxX, glyphEnd),
                            MinY = Math.Min(currentMinY, glyphLower),
                            MaxY = Math.Max(currentMaxY, glyphUpper),
                        };
                    }
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
                var fontStack = state.CurrentFontStack;
                x += ResolveMeasurement(posElement.Value, fontStack.Size, 0.0f);
            }

            if (element is SizeElement sizeElement)
            {
                state.CurrentSize = sizeElement.Value;
            }
            
            if (element is VOffsetElement vOffsetElement)
            {
                state.CurrentVOffset = vOffsetElement.Value;
            }
            
            if (element is LineHeightElement lineHeightElement)
            {
                state.CurrentLineHeight = lineHeightElement.Value;
            }

            if (element is MarkElement markElement)
            {
                // Add the current mark to the list if it has a width and height
                if (currentMark.HasValue && currentMark.Value.Width != 0.0f && currentMark.Value.Height != 0.0f)
                    marks.Add(currentMark.Value);

                var hasMark = markElement.Value.Rgb.HasValue;
                currentMark = hasMark
                    ? new Mark(x, 0.0f, x, 0.0f, markElement.Value)
                    : null;
                state.CurrentMarkColor = markElement.Value;
            }

            if (element is FontElement fontElement)
            {
                var fontName = fontElement.Value?.ToLowerInvariant();
                if (!string.IsNullOrEmpty(fontName) && fontStackLookup.TryGetValue(fontName, out var fontStack))
                    state.CurrentFontStack = fontStack;
            }
        }
        
        if (currentMark.HasValue && currentMark.Value.Width != 0.0f && currentMark.Value.Height != 0.0f)
            marks.Add(currentMark.Value);
        
        var lastFontStack = state.CurrentFontStack;
        var firstFontHandle = lastFontStack.Fonts[0];
        var firstFont = registeredFonts[fontIndexLookup[firstFontHandle]];
        var firstFontMetadata = getFontMetadata(firstFont);
        var lastCurrentSize = ResolveMeasurement(state.CurrentSize, lastFontStack.Size, lastFontStack.Size);
        var lastVOffset = ResolveMeasurement(
            state.CurrentVOffset,
            firstFontMetadata.LineHeight / firstFontMetadata.Size * lastCurrentSize,
            firstFontMetadata.LineHeight / firstFontMetadata.Size * lastCurrentSize);
        var normalizedLastAscender = firstFontMetadata.Ascender / firstFontMetadata.Size;
        var normalizedLastDescender = firstFontMetadata.Descender / firstFontMetadata.Size;
        
        ascender = ascender == 0.0f ? Math.Max(normalizedLastAscender * lastCurrentSize, normalizedLastAscender * lastCurrentSize + lastVOffset) : ascender;
        descender = descender == 0.0f ? Math.Min(normalizedLastDescender * lastCurrentSize, normalizedLastDescender * lastCurrentSize + lastVOffset) : descender;
        height = height == 0.0f ? ascender - descender : height;
        
        height = ResolveMeasurement(state.CurrentLineHeight, height, height);
        
        return new TmpLine(ascender, descender, width, height, state.CurrentAlignment, glyphs.ToArray(), marks.ToArray());
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
        {
            var character = getCharacterFromOrdinal(registeredFonts[fontIndexLookup[fontHandle]], c);
            if (character.HasValue)
                return (character.Value.GlyphId, i);
        }
        return null;
    }
    
    private static float GetParagraphHeight(IReadOnlyList<TmpLine> lines)
    {
        if (lines.Count == 0)
            return 0.0f;

        var height = lines[0].Ascender;
        for (var i = 0; i < lines.Count - 1; i++)
            height += lines[i].Height;
        height -= lines[^1].Descender;
        return height;
    }
}