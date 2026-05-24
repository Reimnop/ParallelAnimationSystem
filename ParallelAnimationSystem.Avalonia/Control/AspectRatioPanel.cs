using Avalonia;
using Avalonia.Controls;

namespace ParallelAnimationSystem.Avalonia.Control;

public class AspectRatioPanel : Panel
{
    public static readonly StyledProperty<double> RatioProperty =
        AvaloniaProperty.Register<AspectRatioPanel, double>(nameof(Ratio), defaultValue: 16.0 / 9.0);

    public double Ratio
    {
        get => GetValue(RatioProperty);
        set => SetValue(RatioProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = ConstrainToRatio(availableSize);
        foreach (var child in Children)
            child.Measure(size);
        return size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = ConstrainToRatio(finalSize);
        foreach (var child in Children)
            child.Arrange(new Rect(size));
        return size;
    }

    private Size ConstrainToRatio(Size available)
    {
        if (available.Width == 0 || available.Height == 0)
            return available;

        var heightFromWidth = available.Width / Ratio;
        return heightFromWidth <= available.Height
            ? new Size(available.Width, heightFromWidth)
            : new Size(available.Height * Ratio, available.Height);
    }
}