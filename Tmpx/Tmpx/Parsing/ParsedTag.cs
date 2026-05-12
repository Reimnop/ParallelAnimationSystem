namespace Tmpx.Parsing;

public record ParsedTag(
    bool IsClosing,
    string Name, // "font", "color", "b", etc.
    string? Value, // the "primary" value directly on the name, e.g. #FF0000
    string Raw,
    Dictionary<string, string> Attributes  // extra kv pairs
);