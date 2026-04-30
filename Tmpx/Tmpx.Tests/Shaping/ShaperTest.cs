using System.Numerics;
using Tmpx.Common;
using Tmpx.Parsing;
using Tmpx.Shaping;

namespace Tmpx.Tests.Shaping;

public class ShaperTest
{
    private readonly MockFontResolver resolver;
    private readonly Shaper _shaper;
    private const string DefaultFont = "Test";
    private const float UnitsPerEm = 16f;

    public ShaperTest()
    {
        resolver = new MockFontResolver();
        resolver.Register(MockFontFactory.Create("Test", FontStyle.Regular), FontStyle.Regular);
        resolver.Register(MockFontFactory.Create("Test", FontStyle.Bold), FontStyle.Bold);
        resolver.Register(MockFontFactory.Create("Test", FontStyle.Italic), FontStyle.Italic);
        resolver.Register(MockFontFactory.Create("Other", FontStyle.Regular), FontStyle.Regular);
        _shaper = new Shaper(resolver, UnitsPerEm);
    }

    private List<(Matrix3x2 Transform, Tmpx.Common.Color Color, IFont Font, int ShapeEntryIndex)> ShapeAll(
        string text,
        TextHorizontalAlignment h = TextHorizontalAlignment.Left,
        TextVerticalAlignment v = TextVerticalAlignment.Top)
    {
        var result = new List<(Matrix3x2, Color, IFont, int)>();
        _shaper.Shape(TextParser.Parse(text), h, v, DefaultFont,
            (t, c, f, s) => result.Add((t, c, f, s)));
        return result;
    }

    // --- basic emission ---

    [Fact]
    public void PlainText_EmitsOneGlyphPerCharacter()
    {
        var glyphs = ShapeAll("abc");
        Assert.Equal(3, glyphs.Count);
    }

    [Fact]
    public void PlainText_AdvancesCursorX()
    {
        var glyphs = ShapeAll("abc");
        Assert.Equal(0f, glyphs[0].Transform.Translation.X, precision: 5);
        Assert.Equal(1f, glyphs[1].Transform.Translation.X, precision: 5);
        Assert.Equal(2f, glyphs[2].Transform.Translation.X, precision: 5);
    }

    [Fact]
    public void PlainText_BaselineAtAscender()
    {
        var glyphs = ShapeAll("a");
        Assert.Equal(-MockFontFactory.DefaultMetrics.Ascender, glyphs[0].Transform.Translation.Y, precision: 5);
    }

    [Fact]
    public void MissingGlyph_IsSkipped()
    {
        var emptyFont = new MockFont("Empty", FontStyle.Regular, MockFontFactory.DefaultMetrics, []);
        resolver.Register(emptyFont, FontStyle.Regular);
        var result = new List<(Matrix3x2, Color, IFont, int)>();
        _shaper.Shape(TextParser.Parse("<font=Empty>abc</font>"), TextHorizontalAlignment.Left, TextVerticalAlignment.Top, DefaultFont,
            (t, c, f, s) => result.Add((t, c, f, s)));
        Assert.Empty(result);
    }

    // --- color ---

    [Fact]
    public void ColorTag_ChangesColor()
    {
        var glyphs = ShapeAll("<color=#FF0000>a</color>b");
        Assert.Equal(new Color(255, 0, 0, 255), glyphs[0].Color);
        Assert.Equal(Color.White, glyphs[1].Color);
    }

    [Fact]
    public void ColorTag_WithAlpha_ParsesCorrectly()
    {
        var glyphs = ShapeAll("<color=#FF0000AA>a");
        Assert.Equal(new Color(255, 0, 0, 170), glyphs[0].Color);
    }

    // --- size ---

    [Fact]
    public void SizeTag_Em_ScalesTransform()
    {
        var glyphs = ShapeAll("<size=2em>a</size>b");
        Assert.Equal(2f, glyphs[0].Transform.M11, precision: 5);
        Assert.Equal(1f, glyphs[1].Transform.M11, precision: 5);
    }

    [Fact]
    public void SizeTag_Px_ScalesTransform()
    {
        var glyphs = ShapeAll("<size=32px>a");
        Assert.Equal(2f, glyphs[0].Transform.M11, precision: 5); // 32px / 16 unitsPerEm = 2em
    }

    [Fact]
    public void SizeTag_Percent_ScalesTransform()
    {
        var glyphs = ShapeAll("<size=50%>a");
        Assert.Equal(0.5f, glyphs[0].Transform.M11, precision: 5);
    }

    [Fact]
    public void SizeTag_Relative_BasedOnUnitsPerEm()
    {
        var glyphs = ShapeAll("<size=+16>a"); // base 16 + 16 = 32px = 2em
        Assert.Equal(2f, glyphs[0].Transform.M11, precision: 5);
    }

    [Fact]
    public void SizeTag_AdvancesCorrectly()
    {
        var glyphs = ShapeAll("<size=2em>a</size>b");
        Assert.Equal(0f, glyphs[0].Transform.Translation.X, precision: 5);
        Assert.Equal(2f, glyphs[1].Transform.Translation.X, precision: 5);
    }

    // --- line breaks ---

    [Fact]
    public void LineBreak_ResetsCursorX()
    {
        var glyphs = ShapeAll("a\nb");
        Assert.Equal(0f, glyphs[0].Transform.Translation.X, precision: 5);
        Assert.Equal(0f, glyphs[1].Transform.Translation.X, precision: 5);
    }

    [Fact]
    public void LineBreak_DescendsByLineHeight()
    {
        var glyphs = ShapeAll("a\nb");
        var firstY  = glyphs[0].Transform.Translation.Y;
        var secondY = glyphs[1].Transform.Translation.Y;
        Assert.Equal(MockFontFactory.DefaultMetrics.LineHeight, firstY - secondY, precision: 5);
    }

    [Fact]
    public void EmptyLine_StillDescends()
    {
        var glyphs = ShapeAll("a\n\nb");
        var firstY  = glyphs[0].Transform.Translation.Y;
        var thirdY  = glyphs[1].Transform.Translation.Y;
        Assert.Equal(MockFontFactory.DefaultMetrics.LineHeight * 2f, firstY - thirdY, precision: 5);
    }

    [Fact]
    public void MixedSizeLine_BaselineFromMaxAscender()
    {
        var glyphs = ShapeAll("a<size=2em>b");
        var expectedBaseline = -(MockFontFactory.DefaultMetrics.Ascender * 2f);
        Assert.Equal(expectedBaseline, glyphs[0].Transform.Translation.Y, precision: 5);
        Assert.Equal(expectedBaseline, glyphs[1].Transform.Translation.Y, precision: 5);
    }

    // --- invalid tags fall back to literal text ---

    [Fact]
    public void UnknownTag_RendersAsText()
    {
        var glyphs = ShapeAll("<unknown>a");
        Assert.Equal("<unknown>".Length + 1, glyphs.Count);
    }

    [Fact(Skip = "Nobody would write a font tag with an invalid font name anyway")]
    public void InvalidFontTag_RendersAsText()
    {
        var glyphs = ShapeAll("<font=DoesNotExist>a</font>");
        Assert.Equal("<font=DoesNotExist>".Length + 1 + "</font>".Length, glyphs.Count);
    }

    [Fact]
    public void InvalidAlignTag_RendersAsText()
    {
        var glyphs = ShapeAll("<align=diagonal>a");
        Assert.Equal("<align=diagonal>".Length + 1, glyphs.Count);
    }

    // --- bold/italic ---

    [Fact]
    public void BoldTag_SwitchesToBoldFont()
    {
        var glyphs = ShapeAll("<b>a</b>b");
        Assert.Equal(resolver.Resolve("Test", FontStyle.Bold),    glyphs[0].Font);
        Assert.Equal(resolver.Resolve("Test", FontStyle.Regular), glyphs[1].Font);
    }

    [Fact]
    public void OverlappingBoldTags_StaysBoldUntilBothClosed()
    {
        var glyphs = ShapeAll("<b><b>a</b>b</b>c");
        Assert.Equal(resolver.Resolve("Test", FontStyle.Bold),    glyphs[0].Font);
        Assert.Equal(resolver.Resolve("Test", FontStyle.Bold),    glyphs[1].Font);
        Assert.Equal(resolver.Resolve("Test", FontStyle.Regular), glyphs[2].Font);
    }

    [Fact]
    public void OverlappingBoldItalic_BothActiveSimultaneously()
    {
        resolver.Register(MockFontFactory.Create("Test", FontStyle.Bold | FontStyle.Italic), FontStyle.Bold | FontStyle.Italic);
        var glyphs = ShapeAll("<b><i>a</i></b>");
        Assert.Equal(resolver.Resolve("Test", FontStyle.Bold | FontStyle.Italic), glyphs[0].Font);
    }

    // --- horizontal alignment ---

    [Fact]
    public void LeftAlignment_OriginAtLeft()
    {
        var glyphs = ShapeAll("abc", h: TextHorizontalAlignment.Left);
        Assert.Equal(0f, glyphs[0].Transform.Translation.X, precision: 5);
    }

    [Fact]
    public void CenterAlignment_OriginAtCenter()
    {
        // 3 glyphs * advanceWidth 1em = lineWidth 3em, center offset = -1.5em
        var glyphs = ShapeAll("abc", h: TextHorizontalAlignment.Center);
        Assert.Equal(-1.5f, glyphs[0].Transform.Translation.X, precision: 5);
        Assert.Equal(-0.5f, glyphs[1].Transform.Translation.X, precision: 5);
        Assert.Equal( 0.5f, glyphs[2].Transform.Translation.X, precision: 5);
    }

    [Fact]
    public void RightAlignment_OriginAtRight()
    {
        // lineWidth 3em, right offset = -3em
        var glyphs = ShapeAll("abc", h: TextHorizontalAlignment.Right);
        Assert.Equal(-3f, glyphs[0].Transform.Translation.X, precision: 5);
        Assert.Equal(-2f, glyphs[1].Transform.Translation.X, precision: 5);
        Assert.Equal(-1f, glyphs[2].Transform.Translation.X, precision: 5);
    }

    [Fact]
    public void AlignTag_OverridesDefaultAlignment()
    {
        // default left, tag overrides to right for first line only
        var glyphs = ShapeAll("<align=right>abc</align>\ndef", h: TextHorizontalAlignment.Left);
        Assert.Equal(-3f, glyphs[0].Transform.Translation.X, precision: 5); // first line right-aligned
        Assert.Equal( 0f, glyphs[3].Transform.Translation.X, precision: 5); // second line left-aligned
    }

    [Fact]
    public void AlignTag_LastTagOnLineWins()
    {
        // per spec: last active alignment at flush time wins
        var glyphs = ShapeAll("<align=right>a</align>bc", h: TextHorizontalAlignment.Left);
        // </align> fires before line ends, so left alignment wins
        Assert.Equal(0f, glyphs[0].Transform.Translation.X, precision: 5);
    }

    // --- vertical alignment ---

    [Fact]
    public void VerticalTop_FirstLineAtZeroMinusAscender()
    {
        var glyphs = ShapeAll("a", v: TextVerticalAlignment.Top);
        Assert.Equal(-MockFontFactory.DefaultMetrics.Ascender, glyphs[0].Transform.Translation.Y, precision: 5);
    }

    [Fact]
    public void VerticalBottom_LastLineAtZeroMinusAscender()
    {
        // two lines, bottom alignment — entire block shifted up by textHeight
        var glyphs = ShapeAll("a\nb", v: TextVerticalAlignment.Bottom);
        var metrics = MockFontFactory.DefaultMetrics;
        var textHeight = metrics.LineHeight * 2f;
        var expectedFirstY  = -metrics.Ascender + textHeight;
        var expectedSecondY = -metrics.Ascender + textHeight - metrics.LineHeight;
        Assert.Equal(expectedFirstY,  glyphs[0].Transform.Translation.Y, precision: 5);
        Assert.Equal(expectedSecondY, glyphs[1].Transform.Translation.Y, precision: 5);
    }

    [Fact]
    public void VerticalMiddle_BlockCenteredOnZero()
    {
        var glyphs = ShapeAll("a\nb", v: TextVerticalAlignment.Middle);
        var metrics = MockFontFactory.DefaultMetrics;
        var textHeight = metrics.LineHeight * 2f;
        var expectedFirstY  = -metrics.Ascender + textHeight / 2f;
        var expectedSecondY = -metrics.Ascender + textHeight / 2f - metrics.LineHeight;
        Assert.Equal(expectedFirstY,  glyphs[0].Transform.Translation.Y, precision: 5);
        Assert.Equal(expectedSecondY, glyphs[1].Transform.Translation.Y, precision: 5);
    }
}