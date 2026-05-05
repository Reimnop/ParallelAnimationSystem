using System.Text;

namespace Tmpx.Parsing;

public static class TagParser
{
    public static ParsedTag ParseInner(string inner, string raw)
    {
        inner = inner.Trim();
    
        var isClosing = inner.StartsWith('/');
        if (isClosing) inner = inner[1..].TrimStart();
    
        // split on whitespace, but respect quoted values
        var tokens = SplitTokens(inner);
        if (tokens.Count == 0) return new ParsedTag(isClosing, "", null, raw, []);
    
        // first token is always the name, optionally with a value
        string? primaryValue = null;
        var eq = tokens[0].IndexOf('=');
        string name;
        if (eq == -1)
        {
            name = tokens[0];
            
            // next token with no key is the primary value
            if (tokens.Count > 1 && !tokens[1].Contains('='))
                primaryValue = tokens[1];
        }
        else
        {
            (name, primaryValue) = SplitOnFirst(tokens[0], '=');
        }
    
        // remaining tokens are extra attributes
        var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i < tokens.Count; i++)
        {
            var (k, v) = SplitOnFirst(tokens[i], '=');
            if (k.Length > 0) attrs[k] = v ?? "";
        }
    
        return new ParsedTag(isClosing, name, primaryValue, raw, attrs);
    }

    // splits `font="Inconsolata" material="whatever the fuck"` respecting quotes
    private static List<string> SplitTokens(string s)
    {
        var tokens = new List<string>();
        var i = 0;
        while (i < s.Length)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            if (i >= s.Length) break;
        
            var sb = new StringBuilder();
            var inQuote = false;
            var quoteChar = '\0';
        
            while (i < s.Length && (inQuote || !char.IsWhiteSpace(s[i])))
            {
                var c = s[i];
                if (!inQuote && (c == '"' || c == '\''))
                {
                    inQuote = true; quoteChar = c; i++; continue;
                }
                if (inQuote && c == quoteChar)
                {
                    inQuote = false; i++; continue;
                }
                sb.Append(c); i++;
            }
        
            if (sb.Length > 0) tokens.Add(sb.ToString());
        }
        return tokens;
    }

    // "color=#FF0000" -> ("color", "#FF0000"), "b" -> ("b", null)
    private static (string key, string? value) SplitOnFirst(string s, char sep)
    {
        var i = s.IndexOf(sep);
        return i == -1 ? (s, null) : (s[..i], s[(i+1)..]);
    }
}