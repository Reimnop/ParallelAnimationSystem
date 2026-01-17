using TmpIO;

namespace ParallelAnimationSystem.Rendering.OpenGL;

public class Font(int id, TmpFile file) : IFont
{
    public int Id => id;
    public TmpMetadata Metadata { get; } = file.Metadata;

    private readonly Dictionary<char, TmpCharacter> ordinalToCharacter
        = file.Characters.ToDictionary(x => x.Character);

    private readonly Dictionary<int, TmpGlyph> glyphIdToGlyph
        = file.Glyphs.ToDictionary(x => x.Id);
    
    public bool TryGetCharacterFromOrdinal(char ordinal, out TmpCharacter character)
        => ordinalToCharacter.TryGetValue(ordinal, out character);

    public bool TryGetGlyphFromId(int glyphId, out TmpGlyph glyph)
        => glyphIdToGlyph.TryGetValue(glyphId, out glyph);
}