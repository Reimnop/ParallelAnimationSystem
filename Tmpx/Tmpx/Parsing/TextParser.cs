using System.Text.RegularExpressions;

namespace Tmpx.Parsing;

public static partial class TextParser
{
    [GeneratedRegex(@"<([^<>]+?)>")]
    private static partial Regex GetTagRegex();
    
    public static IEnumerable<Token> Parse(string text)
    {
        var regex = GetTagRegex();
        var matches = regex.Matches(text);
        var lastIndex = 0;
        foreach (Match match in matches)
        {
            if (match.Index > lastIndex)
                foreach (var token in BreakLines(text.Substring(lastIndex, match.Index - lastIndex)))
                    yield return token;

            var inner = match.Groups[1].Value.Trim();
            if (string.Equals(inner, "br", StringComparison.OrdinalIgnoreCase))
                yield return new LineBreakToken();
            else
            {
                if (inner.StartsWith('#'))
                    inner = $"color={inner}";
                yield return new TagToken(inner, match.Value);
            }
            
            lastIndex = match.Index + match.Length;
        }
        
        if (lastIndex < text.Length)
            foreach (var token in BreakLines(text.Substring(lastIndex)))
                yield return token;
    }

    private static IEnumerable<Token> BreakLines(string text)
    {
        var lines = text.Split('\n');
        if (lines.Length == 0)
            yield break;
        yield return new TextToken(lines[0]);
        for (var i = 1; i < lines.Length; i++)
        {
            yield return new LineBreakToken();
            yield return new TextToken(lines[i]);
        }
    }
}