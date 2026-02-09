namespace ParallelAnimationSystem.Core.Data;

public class ThemeColorState
{
    public ColorRgb Background { get; set; }
    public ColorRgb Gui { get; set; }
    public ColorRgb GuiAccent { get; set; }
    public ColorRgb[] Player { get; } = new ColorRgb[4];
    public ColorRgb[] Object { get; } = new ColorRgb[9];
    public ColorRgb[] Effect { get; } = new ColorRgb[9];
    public ColorRgb[] ParallaxObject { get; } = new ColorRgb[9];
}