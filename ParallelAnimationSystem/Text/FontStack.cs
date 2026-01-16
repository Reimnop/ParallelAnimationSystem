using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem.Text;

public class FontStack(string name, float size, IEnumerable<IFont> fonts)
{
    public string Name { get; } = name;
    public float Size { get; } = size;
    public IReadOnlyList<IFont> Fonts { get; } = fonts.ToList();
}