using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem.Core.Text;

public class FontStack(string name, float size, IEnumerable<FontHandle> fonts)
{
    public string Name { get; } = name;
    public float Size { get; } = size;
    public IReadOnlyList<FontHandle> Fonts { get; } = fonts.ToList();
}