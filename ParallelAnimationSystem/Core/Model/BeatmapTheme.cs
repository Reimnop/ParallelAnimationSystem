using System.ComponentModel;
using System.Runtime.CompilerServices;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Model;

public class BeatmapTheme : IStringIdentifiable, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<BeatmapThemeColorUpdatedEventArgs>? PlayerColorUpdated;
    public event EventHandler<BeatmapThemeColorUpdatedEventArgs>? ObjectColorUpdated;
    public event EventHandler<BeatmapThemeColorUpdatedEventArgs>? EffectColorUpdated;
    public event EventHandler<BeatmapThemeColorUpdatedEventArgs>? ParallaxObjectColorUpdated;

    public string Id { get; }

    public string Name
    {
        get;
        set => SetField(ref field, value);
    } = string.Empty;

    public ColorRgb BackgroundColor
    {
        get;
        set => SetField(ref field, value);
    }

    public ColorRgb GuiColor
    {
        get;
        set => SetField(ref field, value);
    }

    public ColorRgb GuiAccentColor
    {
        get;
        set => SetField(ref field, value);
    }

    public BeatmapThemeColorList PlayerColors { get; } = new(4);
    public BeatmapThemeColorList ObjectColors { get; } = new(9);
    public BeatmapThemeColorList EffectColors { get; } = new(9);
    public BeatmapThemeColorList ParallaxObjectColors { get; } = new(9);

    public BeatmapTheme(string id)
    {
        Id = id;
        
        PlayerColors.Updated += (_, e) => PlayerColorUpdated?.Invoke(this, e);
        ObjectColors.Updated += (_, e) => ObjectColorUpdated?.Invoke(this, e);
        EffectColors.Updated += (_, e) => EffectColorUpdated?.Invoke(this, e);
        ParallaxObjectColors.Updated += (_, e) => ParallaxObjectColorUpdated?.Invoke(this, e);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}