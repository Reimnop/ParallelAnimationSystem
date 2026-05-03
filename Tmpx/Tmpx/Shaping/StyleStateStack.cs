using Tmpx.Common;

namespace Tmpx.Shaping;

public class StyleStateStack(
    IFontResolver fontResolver,
    FontFallbackChainRegistry fallbackChainRegistry,
    string defaultFontName,
    TextHorizontalAlignment defaultAlignment)
{
    private readonly Stack<TextHorizontalAlignment> alignments = new();
    private readonly Stack<string> fontNames = new();
    private readonly Stack<float> sizes = new();
    private readonly Stack<float> cspaces = new();
    private readonly Stack<float> mspaces = new();
    private readonly Stack<float> voffsets = new();
    private readonly Stack<Color> colors = new();
    private readonly Stack<Color> marks = new();
    private readonly Stack<TextTransform> textTransforms = new();
    private int boldDepth;
    private int italicDepth;
    
    private GlyphTransform currentTransform = GlyphTransform.Identity;
    
    public IReadOnlyList<IFont> CurrentFontChain
    {
        get
        {
            if (cachedFontChain != null)
                return cachedFontChain; 
            
            var fontNames = fallbackChainRegistry.GetFontNames(CurrentFontName);
            cachedFontChain = fontNames
                .Select(family => ResolveFont(family, CurrentFontStyle))
                .ToList();
            
            return cachedFontChain;
        }
    }
    
    public IFont CurrentFont => CurrentFontChain[0];
    
    public GlyphTransform CurrentTransform => currentTransform;
    public TextHorizontalAlignment CurrentAlignment => alignments.TryPeek(out var a) ? a : defaultAlignment;
    public string CurrentFontName => fontNames.TryPeek(out var f) ? f : defaultFontName;
    public float CurrentSize => sizes.TryPeek(out var s) ? s : 1f;
    public float CurrentCSpace => cspaces.TryPeek(out var c) ? c : 0f;
    public float? CurrentMSpace => mspaces.TryPeek(out var m) ? m : null;
    public float CurrentVOffset => voffsets.TryPeek(out var v) ? v : 0f;
    public float? CurrentLineHeight => lineHeight;

    public Color CurrentColor
    {
        get
        {
            var c = colors.TryPeek(out var color) ? color : Color.White;
            return new Color(c.R, c.G, c.B, Math.Min(c.A, currentAlpha));
        }
    }
    
    public Color? CurrentMarkColor => marks.TryPeek(out var m) ? m : null;
    
    public byte CurrentAlpha => currentAlpha;
    public TextTransform CurrentTextTransform => textTransforms.TryPeek(out var t) ? t : TextTransform.None;
    public FontStyle CurrentFontStyle =>
        (boldDepth   > 0 ? FontStyle.Bold   : FontStyle.Regular) |
        (italicDepth > 0 ? FontStyle.Italic : FontStyle.Regular);

    private IReadOnlyList<IFont>? cachedFontChain;
    private float? lineHeight;
    private byte currentAlpha = 255;

    private IFont ResolveFont(string family, FontStyle style)
    {
        if (fontResolver.TryResolve(family, style, out var font))
            return font;
        
        // can't find font, find its regular style as a fallback
        if (style != FontStyle.Regular && fontResolver.TryResolve(family, FontStyle.Regular, out font))
            return font;
        
        throw new InvalidOperationException($"Could not resolve font '{family}'");
    }
    
    public void SetTransform(GlyphTransform transform) 
        => currentTransform = transform;
    
    public void ClearTransform()
        => currentTransform = GlyphTransform.Identity;
    
    public void PushAlignment(TextHorizontalAlignment alignment) 
        => alignments.Push(alignment);
    
    public void PopAlignment()
    {
        if (alignments.Count > 0) 
            alignments.Pop();
    }

    public void PushBold()
    {
        boldDepth++;
        cachedFontChain = null;
    }

    public void PopBold()
    {
        if (boldDepth > 0)
            boldDepth--;
        cachedFontChain = null;
    }

    public void PushItalic()
    {
        italicDepth++;
        cachedFontChain = null;
    }

    public void PopItalic()
    {
        if (italicDepth > 0)
            italicDepth--;
        cachedFontChain = null;
    }

    public void PushColor(Color color) 
        => colors.Push(color);

    public void PopColor()
    {
        if (colors.Count > 0) 
            colors.Pop();
    }
    
    public void SetAlpha(byte alpha)
        => currentAlpha = alpha;
    
    public void PushMark(Color color) 
        => marks.Push(color);

    public void PopMark()
    {
        if (marks.Count > 0) 
            marks.Pop();
    }
    
    public void PushTextTransform(TextTransform textTransform) 
        => textTransforms.Push(textTransform);
    
    public void PopTextTransform()
    {
        if (textTransforms.Count > 0)
            textTransforms.Pop();
    }

    public void PushSize(float size)   
        => sizes.Push(size);

    public void PopSize()
    {
        if (sizes.Count  > 0) 
            sizes.Pop();
    }
    
    public void PushCSpace(float cspace)
        => cspaces.Push(cspace);

    public void PopCSpace()
    {
        if (cspaces.Count > 0) 
            cspaces.Pop();
    }
    
    public void PushMSpace(float mspace)
        => mspaces.Push(mspace);
    
    public void PopMSpace()
    {
        if (mspaces.Count > 0)
            mspaces.Pop();
    }
    
    public void PushVOffset(float voffset)
        => voffsets.Push(voffset);
    
    public void PopVOffset()
    {
        if (voffsets.Count > 0)
            voffsets.Pop();
    }
    
    public void SetLineHeight(float h) 
        => lineHeight = h;
    
    public void ClearLineHeight() 
        => lineHeight = null;

    public void PushFontName(string fontName)
    {
        fontNames.Push(fontName);
        cachedFontChain = null;
    }

    public void PopFontName()
    {
        if (fontNames.Count > 0) 
            fontNames.Pop();
        cachedFontChain = null;
    }
}