using System.Globalization;
using System.Numerics;
using Tmpx.Common;
using Tmpx.Parsing;

namespace Tmpx.Shaping;

public delegate void ShapeEmitter(Matrix3x2 transform, Color color, IFont? font, int shapeEntryIndex);

public class Shaper(IFontResolver resolver, FontFallbackChainRegistry fallbackChainRegistry, float unitsPerEm = 16f)
{
    private static readonly Dictionary<string, Color> KnownColors = new()
    {
        ["black"] = Color.ParseHex("000000"),
        ["blue"] = Color.ParseHex("0000FF"),
        ["green"] = Color.ParseHex("00FF00"),
        ["orange"] = Color.ParseHex("FFA500"),
        ["purple"] = Color.ParseHex("800080"),
        ["red"] = Color.ParseHex("FF0000"),
        ["white"] = Color.ParseHex("FFFFFF"),
        ["yellow"] = Color.ParseHex("FFFF00"),
    };
    
    private class PendingGlyph
    {
        public required Vector2 Position { get; set; }
        public required Color Color { get; set; }
        public required IFont Font { get; set; }
        public required IGlyph Glyph { get; set; }
        public required float Size { get; set; }
        public required GlyphTransform Transform { get; set; }
        public required Color? MarkColor { get; set; }
    }

    private class Line
    {
        public List<PendingGlyph> Glyphs { get; } = [];
        public float BaselineY { get; set; }
        public float MaxAscender { get; set; } = float.NegativeInfinity;
        public float MaxDescender { get; set; } = float.PositiveInfinity;
        public float CursorX { get; set; }
        public TextHorizontalAlignment LastAlignment { get; set; }
        public float? LastLineHeight { get; set; }
    }
    
    public void Shape(
        IEnumerable<Token> tokens, 
        TextHorizontalAlignment horizontalAlignment,
        TextVerticalAlignment verticalAlignment,
        string defaultFontName,
        ShapeEmitter emit)
    {
        var tokenList = tokens.ToList();
        if (tokenList.Count > 0 && tokenList[^1] is LineBreakToken)
            tokenList.RemoveAt(tokenList.Count - 1);
        
        var stack = new StyleStateStack(
            resolver,
            fallbackChainRegistry,
            defaultFontName,
            horizontalAlignment);
        var cursorY = 0f;
        
        var collectedLines = new List<Line>();
        
        var line = new Line();
        foreach (var token in tokenList)
        {
            switch (token)
            {
                case TextToken text:
                    ShapeText(text.Text, stack, line);
                    break;

                case TagToken tag:
                    if (!ApplyTag(tag.Parsed, stack, line))
                        ShapeText(tag.Raw, stack, line);
                    break;

                case LineBreakToken:
                    FinishLine(stack, line, ref cursorY);
                    collectedLines.Add(line);
                    line = new Line();
                    break;
            }
        }
        
        FinishLine(stack, line, ref cursorY);
        collectedLines.Add(line);
        
        var firstLine = collectedLines.FirstOrDefault();
        var lastLine  = collectedLines.LastOrDefault();
        
        var firstLineAscender = firstLine != null ? firstLine.MaxAscender + firstLine.BaselineY : 0f;
        var lastLineDescender = lastLine != null ? lastLine.MaxDescender + lastLine.BaselineY : 0f;
        var textHeight = firstLineAscender - lastLineDescender;
        
        var offsetY = float.Lerp(textHeight, 0f, (int)verticalAlignment * 0.5f); // in case we need fractional alignment later...
        
        foreach (var l in collectedLines)
        {
            foreach (var g in l.Glyphs)
                g.Position += new Vector2(0f, offsetY);
        }

        FlushMarkRects(collectedLines, emit);
        FlushAllLines(collectedLines, emit);
    }
    
    private void FlushMarkRects(List<Line> collectedLines, ShapeEmitter emit)
    {
        foreach (var line in collectedLines)
        {
            Color? currentMarkColor = null;
            var runStartX = 0f;
            var runEndX = 0f;
            var runMinY = float.PositiveInfinity;
            var runMaxY = float.NegativeInfinity;

            void EmitCurrentRun()
            {
                if (currentMarkColor is not { } markColor) 
                    return;
                var width  = runEndX - runStartX;
                var height = runMaxY - runMinY;
                if (width <= 0 || height <= 0) 
                    return;
                var transform = Matrix3x2.CreateScale(width, height) * Matrix3x2.CreateTranslation(runStartX, runMinY);
                emit(transform, markColor, null, -1);
            }

            foreach (var g in line.Glyphs)
            {
                if (!Nullable.Equals(g.MarkColor, currentMarkColor))
                {
                    EmitCurrentRun();

                    if (g.MarkColor is { } newColor)
                    {
                        currentMarkColor = newColor;
                        runStartX = g.Position.X;
                        runEndX = g.Position.X + g.Glyph.AdvanceWidth * g.Size * g.Transform.ScaleX;
                        runMinY = g.Position.Y + g.Font.Metrics.Descender * g.Size;
                        runMaxY = g.Position.Y + g.Font.Metrics.Ascender * g.Size;
                    }
                    else
                    {
                        currentMarkColor = null;
                    }
                }
                else if (g.MarkColor != null)
                {
                    // extend run
                    runEndX = g.Position.X + g.Glyph.AdvanceWidth * g.Size * g.Transform.ScaleX;
                    runMinY = MathF.Min(runMinY, g.Position.Y + g.Font.Metrics.Descender * g.Size);
                    runMaxY = MathF.Max(runMaxY, g.Position.Y + g.Font.Metrics.Ascender * g.Size);
                }
            }

            EmitCurrentRun(); // flush final run
        }
    }

    private void FlushAllLines(List<Line> collectedLines, ShapeEmitter emit)
    {
        foreach (var line in collectedLines)
        {
            foreach (var g in line.Glyphs)
            {
                if (g.Glyph.ShapeEntryIndex < 0)
                    continue;
                
                var centerX = g.Glyph.AdvanceWidth * 0.5f;
                var centerY = (g.Font.Metrics.Ascender + g.Font.Metrics.Descender) * 0.5f;
                
                var transform = 
                    Matrix3x2.CreateTranslation(-centerX, -centerY)
                    * Matrix3x2.CreateRotation(g.Transform.Rotation)
                    * Matrix3x2.CreateTranslation(centerX, centerY)
                    * Matrix3x2.CreateScale(g.Size * g.Transform.ScaleX, g.Size) 
                    * Matrix3x2.CreateTranslation(g.Position);
                emit(transform, g.Color, g.Font, g.Glyph.ShapeEntryIndex);
            }
        }
    }

    private void ShapeText(string text, StyleStateStack stack, Line line)
    {
        var fontChain = stack.CurrentFontChain;
        var size = stack.CurrentSize;
        var color = stack.CurrentColor;

        foreach (var c in text)
        {
            var transform = stack.CurrentTextTransform;
            var lookupChar = transform switch
            {
                TextTransform.UpperCase => char.ToUpper(c),
                TextTransform.LowerCase => char.ToLower(c),
                TextTransform.SmallCaps => char.ToUpper(c),
                _ => c
            };

            var smallCapsMultiplier = transform == TextTransform.SmallCaps && char.IsLower(c) ? 0.8f : 1f;

            // Walk the fallback chain: use the first font in the chain that has a glyph for this character
            IFont? font = null;
            IGlyph? glyph = null;
            foreach (var candidate in fontChain)
            {
                if (candidate.TryGetGlyph(lookupChar, out var candidateGlyph))
                {
                    font = candidate;
                    glyph = candidateGlyph;
                    break;
                }
            }

            if (font is null || glyph is null)
                continue;

            var effectiveSize = size * smallCapsMultiplier;
            
            line.Glyphs.Add(new PendingGlyph
            {
                Position = new Vector2(line.CursorX, stack.CurrentVOffset),
                Color = color,
                Font = font,
                Glyph = glyph,
                Size = effectiveSize,
                Transform = stack.CurrentTransform,
                MarkColor = stack.CurrentMarkColor
            });
            line.MaxAscender = Math.Max(line.MaxAscender, font.Metrics.Ascender * effectiveSize + stack.CurrentVOffset);
            line.MaxDescender = Math.Min(line.MaxDescender, font.Metrics.Descender * effectiveSize + stack.CurrentVOffset);
            line.LastAlignment = stack.CurrentAlignment;

            float advance;
            if (stack.CurrentMSpace is { } mspace)
            {
                var monoAdvance = mspace / 2f - glyph.AdvanceWidth / 2f * effectiveSize; // center glyph in slot
                line.Glyphs[^1].Position += new Vector2(monoAdvance, 0f);  // shift glyph position
                advance = mspace + stack.CurrentCSpace;
            }
            else
            {
                advance = glyph.AdvanceWidth * effectiveSize * stack.CurrentTransform.ScaleX + stack.CurrentCSpace;
            }
            line.CursorX += advance;
            
            line.LastLineHeight = stack.CurrentLineHeight;
        }
    }

    private bool ApplyTag(ParsedTag tag, StyleStateStack stack, Line line)
    {
        switch (tag.Name.ToLowerInvariant())
        {
            case "b":
                if (tag.IsClosing) 
                    stack.PopBold();
                else 
                    stack.PushBold();
                return true;
            case "i":
                if (tag.IsClosing) 
                    stack.PopItalic();
                else 
                    stack.PushItalic();
                return true;
            case "u":
                // Underline is recognized but not rendered. Consume the tag silently so it doesn't
                // get echoed back as raw text by the fallback path in Shape().
                return true;
            case "color":
                if (tag.IsClosing)
                {
                    stack.PopColor();
                    return true; 
                }
                if (!TryParseColor(tag.Value, out var color)) 
                    return false;
                stack.PushColor(color);
                return true;
            case "alpha":
                if (tag.IsClosing) 
                    return true; // </alpha> silently consumed
                if (string.IsNullOrWhiteSpace(tag.Value))
                    return false;
                
                var hexValue = tag.Value;
                if (hexValue.StartsWith('#'))
                    hexValue = hexValue[1..];
                
                if (!byte.TryParse(hexValue, NumberStyles.HexNumber, null, out var a)) 
                    return false;
                
                stack.SetAlpha(a);
                return true;
            case "mark":
                if (tag.IsClosing)
                {
                    stack.PopMark(); 
                    return true;
                }

                if (string.IsNullOrWhiteSpace(tag.Value))
                    return true;
                
                if (!TryParseColor(tag.Value, out var markColor)) 
                    return false;
                
                stack.PushMark(markColor);
                return true;
            case "size":
                if (tag.IsClosing) 
                { 
                    stack.PopSize(); 
                    return true; 
                }
                if (!TryParseSize(tag.Value, stack.CurrentSize, out var size)) 
                    return false;
                stack.PushSize(size);
                return true;
            case "font":
                if (tag.IsClosing)
                {
                    stack.PopFontName();
                    return true;
                }
                if (string.IsNullOrWhiteSpace(tag.Value)) 
                    return false;
                stack.PushFontName(tag.Value);
                return true;
            case "material":
                // boo boo bad material tag, consume, ignore
                return true;
            case "align":
                if (tag.IsClosing)
                {
                    stack.PopAlignment();
                    return true;
                }
                if (!Enum.TryParse<TextHorizontalAlignment>(tag.Value, true, out var alignment)) 
                    return false;
                stack.PushAlignment(alignment);
                return true;
            case "pos":
            {
                if (tag.IsClosing)
                    return true; // </pos> does nothing, consumed
                
                if (string.IsNullOrWhiteSpace(tag.Value))
                    return false;
                
                if (tag.Value.EndsWith('%')) 
                    return true; // % consumed but ignored
                
                if (tag.Value.EndsWith("em") && float.TryParse(tag.Value[..^2], out var em))
                {
                    line.CursorX = em * stack.CurrentSize; 
                    return true;
                }

                if (tag.Value.EndsWith("px") && float.TryParse(tag.Value[..^2], out var px))
                {
                    line.CursorX = ToEmSize(px); 
                    return true;
                }

                if (float.TryParse(tag.Value, out var abs))
                {
                    line.CursorX = ToEmSize(abs); 
                    return true;
                }
        
                return false;
            }
            case "space":
            {
                if (tag.IsClosing)
                    return true; // consumed, does nothing
                if (string.IsNullOrWhiteSpace(tag.Value))
                    return false;
                
                if (tag.Value.EndsWith('%')) 
                    return true; // % consumed but ignored
                
                if (tag.Value.EndsWith("em") && float.TryParse(tag.Value[..^2], out var em))
                {
                    line.CursorX += em * stack.CurrentSize; 
                    return true;
                }

                if (tag.Value.EndsWith("px") && float.TryParse(tag.Value[..^2], out var px))
                {
                    line.CursorX += ToEmSize(px); 
                    return true;
                }

                if (float.TryParse(tag.Value, out var abs))
                {
                    line.CursorX += ToEmSize(abs); 
                    return true;
                }
        
                return false;
            }
            case "rotate":
                if (tag.IsClosing)
                {
                    stack.ClearTransform();
                    return true;
                }
                if (!float.TryParse(tag.Value, out var angle)) 
                    return false;
                var radians = MathF.PI * angle / 180f;
                stack.SetTransform(new GlyphTransform(1f, radians));
                return true;
            case "cspace":
            {
                if (tag.IsClosing)
                {
                    line.CursorX -= stack.CurrentCSpace;
                    stack.PopCSpace();
                    return true;
                }
                
                if (string.IsNullOrWhiteSpace(tag.Value))
                    return false;

                if (tag.Value.EndsWith("em") && float.TryParse(tag.Value[..^2], out var em))
                {
                    stack.PushCSpace(em);
                    return true;
                }

                if (tag.Value.EndsWith("px") && float.TryParse(tag.Value[..^2], out var px))
                {
                    stack.PushCSpace(ToEmSize(px));
                    return true;
                }

                if (float.TryParse(tag.Value, out var abs))
                {
                    stack.PushCSpace(ToEmSize(abs));
                    return true;
                }

                return false;
            }
            case "mspace":
            {
                if (tag.IsClosing)
                {
                    stack.PopMSpace();
                    return true;
                }

                if (string.IsNullOrWhiteSpace(tag.Value))
                    return false;

                if (tag.Value.EndsWith("em") && float.TryParse(tag.Value[..^2], out var em))
                {
                    stack.PushMSpace(em * stack.CurrentSize);
                    return true;
                }

                if (tag.Value.EndsWith("px") && float.TryParse(tag.Value[..^2], out var px))
                {
                    stack.PushMSpace(ToEmSize(px));
                    return true;
                }

                if (float.TryParse(tag.Value, out var abs))
                {
                    stack.PushMSpace(ToEmSize(abs));
                    return true;
                }

                return false;
            }
            case "voffset":
            {
                if (tag.IsClosing)
                {
                    stack.PopVOffset();
                    return true;
                }

                if (string.IsNullOrWhiteSpace(tag.Value)) 
                    return false;
                        
                if (tag.Value.EndsWith("em") && float.TryParse(tag.Value[..^2], out var em))
                {
                    stack.PushVOffset(em * stack.CurrentSize);
                    return true;
                }

                if (tag.Value.EndsWith("px") && float.TryParse(tag.Value[..^2], out var px))
                {
                    stack.PushVOffset(ToEmSize(px));
                    return true;
                }

                if (float.TryParse(tag.Value, out var abs))
                {
                    stack.PushVOffset(ToEmSize(abs));
                    return true;
                }

                return false;
            }
            case "line-height":
            {
                if (tag.IsClosing)
                {
                    stack.ClearLineHeight(); 
                    return true; 
                }
                
                if (string.IsNullOrWhiteSpace(tag.Value)) 
                    return false;

                if (tag.Value.EndsWith('%') && float.TryParse(tag.Value[..^1], out var pct))
                {
                    stack.SetLineHeight(stack.CurrentFont.Metrics.LineHeight * stack.CurrentSize * (pct / 100f)); 
                    return true; 
                }

                if (tag.Value.EndsWith("em") && float.TryParse(tag.Value[..^2], out var em))
                {
                    stack.SetLineHeight(em * stack.CurrentSize); 
                    return true; 
                }

                if (tag.Value.EndsWith("px") && float.TryParse(tag.Value[..^2], out var px))
                {
                    stack.SetLineHeight(ToEmSize(px)); 
                    return true; 
                }

                if (float.TryParse(tag.Value, out var abs))
                {
                    stack.SetLineHeight(ToEmSize(abs));
                    return true; 
                }
                
                return false;
            }
            case "uppercase":
            case "allcaps":
                if (tag.IsClosing)
                {
                    stack.PopTextTransform(); 
                    return true; 
                }
                stack.PushTextTransform(TextTransform.UpperCase);
                return true;
            case "lowercase":
                if (tag.IsClosing)
                {
                    stack.PopTextTransform(); 
                    return true; 
                }
                stack.PushTextTransform(TextTransform.LowerCase);
                return true;
            case "smallcaps":
                if (tag.IsClosing) 
                { 
                    stack.PopTextTransform(); 
                    return true; 
                }
                stack.PushTextTransform(TextTransform.SmallCaps);
                return true;
            case "scale":
                if (tag.IsClosing)
                {
                    stack.ClearTransform();
                    return true;
                }
                if (!float.TryParse(tag.Value, out var scale)) 
                    return false;
                stack.SetTransform(new GlyphTransform(scale, 0f));
                return true;
            default:
                return false;
        }
    }
    
    private bool TryParseSize(string? value, float currentSize, out float size)
    {
        size = currentSize;
        
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (value.EndsWith('%') && float.TryParse(value[..^1], out var pct))
        {
            size = pct / 100f;
            return true;
        }

        if (value.EndsWith("em") && float.TryParse(value[..^2], out var em))
        {
            size = em;
            return true;
        }
        
        if (value.EndsWith("px") && float.TryParse(value[..^2], out var px))
        {
            size = ToEmSize(px);
            return true;
        }

        if (value.StartsWith('+') && float.TryParse(value[1..], out var add))
        {
            size = ToEmSize(unitsPerEm + add);
            return true;
        }

        if (value.StartsWith('-') && float.TryParse(value[1..], out var sub))
        {
            size = ToEmSize(unitsPerEm - sub);
            return true;
        }

        if (float.TryParse(value, out var abs))
        {
            size = ToEmSize(abs);
            return true;
        }

        return false;
    }

    private float ToEmSize(float pxSize)
        => pxSize / unitsPerEm;
    
    private static void FinishLine(StyleStateStack stack, Line line, ref float cursorY)
    {
        float lineHeight;
        
        if (line.Glyphs.Count == 0)
        {
            lineHeight = stack.CurrentFont.Metrics.LineHeight * stack.CurrentSize;
            line.MaxAscender = stack.CurrentFont.Metrics.Ascender * stack.CurrentSize;
            line.MaxDescender = stack.CurrentFont.Metrics.Descender * stack.CurrentSize;
            line.BaselineY = cursorY;
            cursorY -= lineHeight;
            return;
        }
        
        lineHeight = line.LastLineHeight ?? GetNaturalLineHeight(line);
        var lineWidth = line.CursorX - stack.CurrentCSpace;

        var offsetX = float.Lerp(0f, -lineWidth, (int)line.LastAlignment * 0.5f); // in case we need fractional alignment later...

        var baseline = cursorY - line.MaxAscender;

        foreach (var g in line.Glyphs)
            g.Position += new Vector2(offsetX, baseline);
        
        line.BaselineY = baseline;
        cursorY -= lineHeight;
    }
    
    private static float GetNaturalLineHeight(Line line)
    {
        if (line.Glyphs.Count == 0)
            return 0f;
    
        var dominant = line.Glyphs.MaxBy(g => g.Size)!;
        var metrics = dominant.Font.Metrics;
        var size = dominant.Size;
    
        var lineGap = metrics.LineHeight * size - (metrics.Ascender - metrics.Descender) * size;
    
        return line.MaxAscender - line.MaxDescender + lineGap;
    }

    private static bool TryParseColor(string hex, out Color color)
    {
        color = default;
        
        if (string.IsNullOrWhiteSpace(hex))
            return false;
        
        if (KnownColors.TryGetValue(hex, out color))
            return true;
        
        if (hex.StartsWith('#'))
            hex = hex[1..];
        
        if (hex.Length != 3 && hex.Length != 4 && hex.Length != 6 && hex.Length != 8)
            return false;
        
        if (hex.Length == 3 || hex.Length == 4)
            hex = string.Concat(hex.Select(c => new string(c, 2)));
        
        if (!Color.TryParseHex(hex[..6], out color))
            return false;

        if (hex.Length == 8)
        {
            if (!byte.TryParse(hex[6..8], NumberStyles.HexNumber, null, out var a))
                return false;
            color.A = a;
        }
        
        return true;
    }
}