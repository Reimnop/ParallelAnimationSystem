using OpenTK.Mathematics;

namespace ParallelAnimationSystem.Core;

public class ThemeColors
{
    public List<Color4> Player { get; set; } = [];
    public List<Color4> Object { get; set; } = [];
    public List<Color4> Effect { get; set; } = [];
    public List<Color4> ParallaxObject { get; set; } = [];
    public Color4 Background { get; set; }
    public Color4 Gui { get; set; }
    public Color4 GuiAccent { get; set; }
}