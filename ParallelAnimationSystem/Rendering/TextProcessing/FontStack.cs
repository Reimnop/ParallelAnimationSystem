namespace ParallelAnimationSystem.Rendering.TextProcessing;

public class FontStack(string name, float size, List<FontHandle> fonts)
{
    public string Name { get; set; } = name;
    public float Size { get; set; } = size;
    public List<FontHandle> Fonts { get; set; } = fonts;
}