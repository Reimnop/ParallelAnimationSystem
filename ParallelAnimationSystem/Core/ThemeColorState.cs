using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core;

public class ThemeColorState
{
    public List<ColorRgba> Player { get; set; } = [];
    public List<ColorRgba> Object { get; set; } = [];
    public List<ColorRgba> Effect { get; set; } = [];
    public List<ColorRgba> ParallaxObject { get; set; } = [];
    public ColorRgba Background { get; set; }
    public ColorRgba Gui { get; set; }
    public ColorRgba GuiAccent { get; set; }
}