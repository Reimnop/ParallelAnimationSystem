namespace ParallelAnimationSystem.Rendering.TextProcessing;

public class FontStack(string name, float size, List<IFontHandle> fonts)
{
    public string Name { get; set; } = name;
    public float Size { get; set; } = size;
    public List<IFontHandle> Fonts { get; set; } = fonts;
}