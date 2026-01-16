using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace ParallelAnimationSystem.Text;

public class FontCollection(IEnumerable<FontStack> fontStacks) : IReadOnlyCollection<FontStack>
{
    public int Count => fonts.Count;
    
    // Font names are case-insensitive
    private readonly Dictionary<string, FontStack> fonts 
        = fontStacks.ToDictionary(fs => fs.Name.ToLowerInvariant(), fs => fs);
    
    public bool TryGetFontStack(string name, [MaybeNullWhen(false)] out FontStack fontStack)
        => fonts.TryGetValue(name.ToLowerInvariant(), out fontStack);
    
    public IEnumerator<FontStack> GetEnumerator()
    {
        return fonts.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}