using Avalonia;
using Avalonia.Controls.Primitives;

namespace ParallelAnimationSystem.Avalonia.Controls;

public sealed class SettingsLabel : TemplatedControl
{
    public static readonly StyledProperty<string> ContentProperty =
        AvaloniaProperty.Register<SettingsLabel, string>(nameof(Content), defaultValue: string.Empty);

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<SettingsLabel, string?>(nameof(Description), defaultValue: null);

    public string Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }
    
    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
}
