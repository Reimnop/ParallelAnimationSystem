using TmpIO;

namespace ParallelAnimationSystem.Rendering;

public class FontData(TmpFile fontFile, Dictionary<char, int> characterToGlyphId, Dictionary<int, TmpGlyph> glyphIdToGlyph)
{ 
    public TmpFile FontFile { get; } = fontFile;
    public Dictionary<char, int> CharacterToGlyphId { get; } = characterToGlyphId;
    public Dictionary<int, TmpGlyph> GlyphIdToGlyph { get; } = glyphIdToGlyph;
    public bool Initialized { get; private set; }
    public int AtlasHandle { get; private set; }

    public void Initialize(int atlasHandle)
    {
        if (Initialized)
            return;
        Initialized = true;
            
        AtlasHandle = atlasHandle;
    }
}