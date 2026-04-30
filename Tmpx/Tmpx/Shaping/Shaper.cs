using System.Globalization;
using System.Numerics;
using Tmpx.Common;
using Tmpx.Parsing;

namespace Tmpx.Shaping;

public delegate void ShapeEmitter(Matrix3x2 transform, Color color, IFont? font, int shapeEntryIndex);

public class Shaper(IFontResolver resolver, float unitsPerEm = 16f)
{
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
        var stack = new StyleStateStack(
            resolver,
            horizontalAlignment,
            defaultFontName);
        var cursorY = 0f;
        
        var collectedLines = new List<Line>();
        
        var line = new Line();
        foreach (var token in tokens)
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
        
        var textHeight = -cursorY;
        var offsetY = float.Lerp(0f, textHeight, (int)verticalAlignment * 0.5f); // in case we need fractional alignment later...
        
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
        var font = stack.CurrentFont;
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
            
            if (!font.TryGetGlyph(lookupChar, out var glyph)) 
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
                var monoAdvance = mspace / 2f - (glyph.AdvanceWidth / 2f + 0f) * effectiveSize; // center glyph in slot
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
                if (tag.Value is not ['#', _, _]) 
                    return false;
                if (!byte.TryParse(tag.Value[1..], NumberStyles.HexNumber, null, out var a)) 
                    return false;
                stack.SetAlpha(a);
                return true;
            case "mark":
                if (tag.IsClosing)
                {
                    stack.PopMark(); 
                    return true;
                }
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
            cursorY -= lineHeight;
            return;
        }
        
        lineHeight = line.LastLineHeight ?? line.MaxAscender - line.MaxDescender;
        var lineWidth = line.CursorX - stack.CurrentCSpace;

        var offsetX = float.Lerp(0f, -lineWidth, (int)line.LastAlignment * 0.5f); // in case we need fractional alignment later...

        var baseline = cursorY - line.MaxAscender;

        foreach (var g in line.Glyphs)
            g.Position += new Vector2(offsetX, baseline);

        cursorY -= lineHeight;
    }

    private static bool TryParseColor(string? value, out Color color)
    {
        color = default;
        if (value == null) 
            return false;

        if (value.StartsWith('#'))
        {
            var hex = value[1..];
            
            if (hex.Length == 6 && uint.TryParse(hex, NumberStyles.HexNumber, null, out var rgb))
            {
                color = new Color((byte)(rgb >> 16), (byte)(rgb >> 8 & 0xFF), (byte)(rgb & 0xFF), 255);
                return true;
            }
            
            if (hex.Length == 8 && uint.TryParse(hex, NumberStyles.HexNumber, null, out var rgba))
            {
                color = new Color((byte)(rgba >> 24), (byte)(rgba >> 16 & 0xFF), (byte)(rgba >> 8 & 0xFF), (byte)(rgba & 0xFF));
                return true;
            }
        }

        return false;
    }
}