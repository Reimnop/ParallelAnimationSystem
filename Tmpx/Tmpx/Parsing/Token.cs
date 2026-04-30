namespace Tmpx.Parsing;

public abstract class Token(string raw)
{
    public string Raw => raw;
}

public class TextToken(string text) : Token(text)
{
    public string Text => Raw;

    public override bool Equals(object? obj)
    {
        return obj is TextToken token && Text == token.Text && Raw == token.Raw;
    }

    protected bool Equals(TextToken other)
    {
        return Text == other.Text && Raw == other.Raw;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Text, Raw);
    }
}

public class TagToken(string inner, string raw) : Token(raw)
{
    public string Inner => inner;
    public ParsedTag Parsed => field ??= TagParser.ParseInner(Inner, Raw);

    public override bool Equals(object? obj)
    {
        return obj is TagToken token && Inner == token.Inner && Raw == token.Raw;
    }
    
    protected bool Equals(TagToken other)
    {
        return Inner == other.Inner && Raw == other.Raw;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Inner, Raw);
    }
}

public class LineBreakToken() : Token("\n")
{
    public override bool Equals(object? obj)
    {
        return obj is LineBreakToken;
    }

    protected bool Equals(LineBreakToken other)
        => true;

    public override int GetHashCode()
    {
        return Raw.GetHashCode();
    }
}