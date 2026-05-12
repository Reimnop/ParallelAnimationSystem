using Tmpx.Parsing;

namespace Tmpx.Tests.Parsing;

public class ParserTest
{
    [Fact]
    public void PlainText_EmitsSingleTextToken()
    {
        var tokens = TextParser.Parse("hello world").ToList();
        Assert.Equal([new TextToken("hello world")], tokens);
    }

    [Fact]
    public void Newline_EmitsLineBreak()
    {
        var tokens = TextParser.Parse("hello\nworld").ToList();
        Assert.Equal([
            new TextToken("hello"),
            new LineBreakToken(),
            new TextToken("world")
        ], tokens);
    }

    [Fact]
    public void ConsecutiveNewlines_EmitsMultipleLineBreaks()
    {
        var tokens = TextParser.Parse("a\n\nb").ToList();
        Assert.Equal([
            new TextToken("a"),
            new LineBreakToken(),
            new TextToken(""),
            new LineBreakToken(),
            new TextToken("b")
        ], tokens);
    }

    [Fact]
    public void SimpleTag_EmitsTagToken()
    {
        var tokens = TextParser.Parse("<b>").ToList();
        var tag = Assert.IsType<TagToken>(Assert.Single(tokens));
        Assert.Equal("b", tag.Parsed.Name);
        Assert.False(tag.Parsed.IsClosing);
        Assert.Null(tag.Parsed.Value);
    }

    [Fact]
    public void ClosingTag_IsMarkedClosing()
    {
        var tokens = TextParser.Parse("</b>").ToList();
        var tag = Assert.IsType<TagToken>(Assert.Single(tokens));
        Assert.True(tag.Parsed.IsClosing);
        Assert.Equal("b", tag.Parsed.Name);
    }

    [Fact]
    public void TagWithValue_ParsesValue()
    {
        var tokens = TextParser.Parse("<color=#FF0000>").ToList();
        var tag = Assert.IsType<TagToken>(Assert.Single(tokens));
        Assert.Equal("color", tag.Parsed.Name);
        Assert.Equal("#FF0000", tag.Parsed.Value);
    }

    [Fact]
    public void TagWithMultipleAttributes_ParsesAll()
    {
        var tokens = TextParser.Parse("<font=\"Inconsolata\" material=\"whatever the fuck\">").ToList();
        var tag = Assert.IsType<TagToken>(Assert.Single(tokens));
        Assert.Equal("font", tag.Parsed.Name);
        Assert.Equal("Inconsolata", tag.Parsed.Value);
        Assert.Equal("whatever the fuck", tag.Parsed.Attributes["material"]);
    }

    [Fact]
    public void TagWithQuotedSpaces_PreservesSpaces()
    {
        var tokens = TextParser.Parse("<font=\"Inconsolata Nerd Emoji\">").ToList();
        var tag = Assert.IsType<TagToken>(Assert.Single(tokens));
        Assert.Equal("Inconsolata Nerd Emoji", tag.Parsed.Value);
    }

    [Fact]
    public void AngleBracketWithNoClose_IsLiteralText()
    {
        var tokens = TextParser.Parse("a < b").ToList();
        Assert.Equal([new TextToken("a < b")], tokens);
    }

    [Fact]
    public void OverlappingTags_AllEmitted()
    {
        var tokens = TextParser.Parse("<b>bold <i>bold italic</b> italic</i>").ToList();
        Assert.Collection(tokens,
            t => Assert.Equal("b",  ((TagToken)t).Parsed.Name),
            t => Assert.Equal("bold ", ((TextToken)t).Text),
            t => Assert.Equal("i",  ((TagToken)t).Parsed.Name),
            t => Assert.Equal("bold italic", ((TextToken)t).Text),
            t => { var tag = (TagToken)t; Assert.Equal("b", tag.Parsed.Name); Assert.True(tag.Parsed.IsClosing); },
            t => Assert.Equal(" italic", ((TextToken)t).Text),
            t => { var tag = (TagToken)t; Assert.Equal("i", tag.Parsed.Name); Assert.True(tag.Parsed.IsClosing); }
        );
    }

    [Theory]
    [InlineData("<size=+10>", "+10")]
    [InlineData("<size=110%>", "110%")]
    [InlineData("<size=1.5em>", "1.5em")]
    public void SizeTag_PreservesRawValue(string input, string expectedValue)
    {
        var tokens = TextParser.Parse(input).ToList();
        var tag = Assert.IsType<TagToken>(Assert.Single(tokens));
        Assert.Equal(expectedValue, tag.Parsed.Value);
    }
}