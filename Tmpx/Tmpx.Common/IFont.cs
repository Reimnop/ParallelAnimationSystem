using System.Diagnostics.CodeAnalysis;

namespace Tmpx.Common;

public interface IFont
{
    string FamilyName { get; }
    string StyleName { get; }
    IFontMetrics Metrics { get; }
    
    bool TryGetGlyph(char c, [MaybeNullWhen(false)] out IGlyph glyph);
    bool TryGetSprite(string name, [MaybeNullWhen(false)] out ISprite sprite);
    float GetKerning(char left, char right);
}