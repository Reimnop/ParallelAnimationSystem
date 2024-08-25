using OpenTK.Mathematics;

namespace ParallelAnimationSystem.Core;

public class ThemeColors
{
    public List<Color4<Rgba>> Player { get; set; } = [];
    public List<Color4<Rgba>> Object { get; set; } = [];
    public List<Color4<Rgba>> Effect { get; set; } = [];
    public List<Color4<Rgba>> ParallaxObject { get; set; } = [];
    public Color4<Rgba> Background { get; set; }
    public Color4<Rgba> Gui { get; set; }
    public Color4<Rgba> GuiAccent { get; set; }
}