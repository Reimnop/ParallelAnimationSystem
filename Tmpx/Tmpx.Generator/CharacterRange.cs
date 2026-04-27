using System.Globalization;

namespace Tmpx.Generator;

public class CharacterRange(char from, char to)
{
    public char From { get; set; } = from;
    public char To { get; set; } = to;

    public override string ToString()
    {
        var hexFrom = ((int)From).ToString("X4");
        var hexTo = ((int)To).ToString("X4");
        return $"U+{hexFrom}-U+{hexTo}";
    }
    
    public IEnumerable<char> Enumerate()
    {
        for (var c = From; c <= To; c++)
            yield return c;
    }

    public static CharacterRange FromString(string s)
    {
        var parts = s.Split('-');
        if (parts.Length > 2)
            throw new FormatException($"Invalid character range format: {s}");

        if (parts.Length == 1)
        {
            var part = parts[0].Trim();
            var character = ParsePart(part);
            return new CharacterRange(character, character);
        }
        else
        {
            var fromPart = parts[0].Trim();
            var toPart = parts[1].Trim();

            var fromChar = ParsePart(fromPart);
            var toChar = ParsePart(toPart);
            
            if (fromChar > toChar)
                throw new FormatException($"Invalid character range format: {s} (from character must be less than or equal to to character)");
            
            return new CharacterRange(fromChar, toChar);
        }
    }
    
    private static char ParsePart(string part)
    {
        if (!part.StartsWith("U+"))
            throw new FormatException($"Invalid character range format: {part}");

        var hex = part.Substring(2);
        if (!int.TryParse(hex, NumberStyles.HexNumber, null, out var codepoint))
            throw new FormatException($"Invalid character range format: {part}");

        return (char)codepoint;
    }
}