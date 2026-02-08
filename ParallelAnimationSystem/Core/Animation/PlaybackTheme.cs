using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Animation;

public class PlaybackTheme(Identifier id) : IIdentifiable
{
    public Identifier Id => id;

    public ColorRgb BackgroundColor { get; set; }
    public ColorRgb GuiColor { get; set; }
    public ColorRgb GuiAccentColor { get; set; }
    public ColorRgb[] PlayerColors { get; } = new ColorRgb[4];
    public ColorRgb[] ObjectColors { get; } = new ColorRgb[9];
    public ColorRgb[] EffectColors { get; } = new ColorRgb[9];
    public ColorRgb[] ParallaxObjectColors { get; } = new ColorRgb[9];
}