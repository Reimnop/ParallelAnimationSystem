using TmpIO;

namespace ParallelAnimationSystem.Core.Text;

public class FontInfo(TmpMetadata metadata, Dictionary<char, TmpCharacter> ordinalToCharacter, Dictionary<int, TmpGlyph> glyphIdToGlyph)
{
    public TmpMetadata Metadata => metadata;
    
    public bool TryGetCharacterFromOrdinal(char ordinal, out TmpCharacter character)
        => ordinalToCharacter.TryGetValue(ordinal, out character);

    public bool TryGetGlyphFromId(int glyphId, out TmpGlyph glyph)
        => glyphIdToGlyph.TryGetValue(glyphId, out glyph);
}