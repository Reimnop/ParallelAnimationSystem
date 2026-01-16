using TmpIO;

namespace ParallelAnimationSystem.Rendering;

public interface IFont
{
    int Id { get; }

    TmpMetadata Metadata { get; }

    bool TryGetCharacterFromOrdinal(char ordinal, out TmpCharacter character);
    bool TryGetGlyphFromId(int glyphId, out TmpGlyph glyph);
}