using System.ComponentModel;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Model;

namespace ParallelAnimationSystem.Core.Service;

public class ThemeManager(PlaybackThemeContainer playbackThemes) : IDisposable
{
    private readonly ThemeSequence themeSequence = new(playbackThemes);
    
    private BeatmapData? attachedBeatmapData;
    
    public void Dispose()
    {
        if (attachedBeatmapData is not null)
            DetachBeatmapData();
    }
    
    public void AttachBeatmapData(BeatmapData beatmapData)
    {
        if (attachedBeatmapData != null)
            throw new InvalidOperationException($"A {nameof(BeatmapData)} is already attached");
        
        attachedBeatmapData = beatmapData;
        
        // add existing themes
        foreach (var theme in beatmapData.Themes.Values)
        {
            var playbackTheme = CreatePlaybackTheme(theme);
            playbackThemes.Insert(playbackTheme);
            
            // attach events
            theme.PropertyChanged += OnBeatmapThemePropertyChanged;
            theme.PlayerColorUpdated += OnBeatmapThemePlayerColorUpdated;
            theme.ObjectColorUpdated += OnBeatmapThemeObjectColorUpdated;
            theme.EffectColorUpdated += OnBeatmapThemeEffectColorUpdated;
            theme.ParallaxObjectColorUpdated += OnBeatmapThemeParallaxObjectColorUpdated;
        }
        
        // attach events
        beatmapData.Themes.Inserted += OnBeatmapThemeInserted;
        beatmapData.Themes.Removed += OnBeatmapThemeRemoved;
        
        // load theme keyframes
        themeSequence.LoadKeyframes(ResolveThemeKeyframes(beatmapData.Events.Theme));
        
        // attach events
        beatmapData.Events.ThemeKeyframesChanged += OnEventsThemeKeyframesChanged;
    }

    public void DetachBeatmapData()
    {
        if (attachedBeatmapData is null)
            return;
        
        foreach (var theme in attachedBeatmapData.Themes.Values)
        {
            // detach events
            theme.PropertyChanged -= OnBeatmapThemePropertyChanged;
            theme.PlayerColorUpdated -= OnBeatmapThemePlayerColorUpdated;
            theme.ObjectColorUpdated -= OnBeatmapThemeObjectColorUpdated;
            theme.EffectColorUpdated -= OnBeatmapThemeEffectColorUpdated;
            theme.ParallaxObjectColorUpdated -= OnBeatmapThemeParallaxObjectColorUpdated;
                
            var index = playbackThemes.GetIndexForId(theme.Id);
            playbackThemes.Remove(index);
        }
            
        // detach events
        attachedBeatmapData.Themes.Inserted -= OnBeatmapThemeInserted;
        attachedBeatmapData.Themes.Removed -= OnBeatmapThemeRemoved;
        
        // unload theme keyframes
        themeSequence.LoadKeyframes([]);
        
        // detach events
        attachedBeatmapData.Events.ThemeKeyframesChanged -= OnEventsThemeKeyframesChanged;
        
        attachedBeatmapData = null;
    }
    
    public ThemeColorState ComputeThemeAt(float time)
        => themeSequence.ComputeValueAt(time);

    private void OnEventsThemeKeyframesChanged(object? sender, KeyframeList<EventKeyframe<string>> e)
    {
        themeSequence.LoadKeyframes(ResolveThemeKeyframes(e));
    }

    private void OnBeatmapThemePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not BeatmapTheme beatmapTheme)
            return;
        
        var index = playbackThemes.GetIndexForId(beatmapTheme.Id);
        if (!playbackThemes.TryGetItem(index, out var playbackTheme))
            return;
        
        switch (e.PropertyName)
        {
            case nameof(BeatmapTheme.BackgroundColor):
                playbackTheme.BackgroundColor = beatmapTheme.BackgroundColor;
                break;
            case nameof(BeatmapTheme.GuiColor):
                playbackTheme.GuiColor = beatmapTheme.GuiColor;
                break;
            case nameof(BeatmapTheme.GuiAccentColor):
                playbackTheme.GuiAccentColor = beatmapTheme.GuiAccentColor;
                break;
        }
    }

    private void OnBeatmapThemePlayerColorUpdated(object? sender, BeatmapThemeColorUpdatedEventArgs e)
    {
        if (sender is not BeatmapTheme beatmapTheme)
            return;
        
        var index = playbackThemes.GetIndexForId(beatmapTheme.Id);
        if (!playbackThemes.TryGetItem(index, out var playbackTheme))
            return;
        
        playbackTheme.PlayerColors[e.Index] = e.NewColor;
    }

    private void OnBeatmapThemeObjectColorUpdated(object? sender, BeatmapThemeColorUpdatedEventArgs e)
    {
        if (sender is not BeatmapTheme beatmapTheme)
            return;
        
        var index = playbackThemes.GetIndexForId(beatmapTheme.Id);
        if (!playbackThemes.TryGetItem(index, out var playbackTheme))
            return;
        
        playbackTheme.ObjectColors[e.Index] = e.NewColor;
    }

    private void OnBeatmapThemeEffectColorUpdated(object? sender, BeatmapThemeColorUpdatedEventArgs e)
    {
        if (sender is not BeatmapTheme beatmapTheme)
            return;
        
        var index = playbackThemes.GetIndexForId(beatmapTheme.Id);
        if (!playbackThemes.TryGetItem(index, out var playbackTheme))
            return;
        
        playbackTheme.EffectColors[e.Index] = e.NewColor;
    }

    private void OnBeatmapThemeParallaxObjectColorUpdated(object? sender, BeatmapThemeColorUpdatedEventArgs e)
    {
        if (sender is not BeatmapTheme beatmapTheme)
            return;
        
        var index = playbackThemes.GetIndexForId(beatmapTheme.Id);
        if (!playbackThemes.TryGetItem(index, out var playbackTheme))
            return;
        
        playbackTheme.ParallaxObjectColors[e.Index] = e.NewColor;
    }

    private void OnBeatmapThemeInserted(object? sender, BeatmapTheme e)
    {
        var playbackTheme = CreatePlaybackTheme(e);
        playbackThemes.Insert(playbackTheme);
        
        // attach events
        e.PropertyChanged += OnBeatmapThemePropertyChanged;
        e.PlayerColorUpdated += OnBeatmapThemePlayerColorUpdated;
        e.ObjectColorUpdated += OnBeatmapThemeObjectColorUpdated;
        e.EffectColorUpdated += OnBeatmapThemeEffectColorUpdated;
        e.ParallaxObjectColorUpdated += OnBeatmapThemeParallaxObjectColorUpdated;
    }

    private void OnBeatmapThemeRemoved(object? sender, BeatmapTheme e)
    {
        e.PropertyChanged -= OnBeatmapThemePropertyChanged;
        e.PlayerColorUpdated -= OnBeatmapThemePlayerColorUpdated;
        e.ObjectColorUpdated -= OnBeatmapThemeObjectColorUpdated;
        e.EffectColorUpdated -= OnBeatmapThemeEffectColorUpdated;
        e.ParallaxObjectColorUpdated -= OnBeatmapThemeParallaxObjectColorUpdated;
    }

    private IEnumerable<Keyframe<int>> ResolveThemeKeyframes(KeyframeList<EventKeyframe<string>> themeEventKeyframes)
    {
        foreach (var eventKeyframe in themeEventKeyframes)
        {
            var themeId = eventKeyframe.Value;
            var index = playbackThemes.GetIndexForId(themeId);
            yield return new Keyframe<int>(eventKeyframe.Time, eventKeyframe.Ease, index);
        }
    } 

    private static PlaybackTheme CreatePlaybackTheme(BeatmapTheme theme)
    {
        var playbackTheme = new PlaybackTheme(theme.Id)
        {
            BackgroundColor = theme.BackgroundColor,
            GuiColor = theme.GuiColor,
            GuiAccentColor = theme.GuiAccentColor
        };
        
        for (var i = 0; i < theme.PlayerColors.Count; i++)
            playbackTheme.PlayerColors[i] = theme.PlayerColors[i];
        
        for (var i = 0; i < theme.ObjectColors.Count; i++)
            playbackTheme.ObjectColors[i] = theme.ObjectColors[i];
        
        for (var i = 0; i < theme.EffectColors.Count; i++)
            playbackTheme.EffectColors[i] = theme.EffectColors[i];
        
        for (var i = 0; i < theme.ParallaxObjectColors.Count; i++)
            playbackTheme.ParallaxObjectColors[i] = theme.ParallaxObjectColors[i];
        
        return playbackTheme;
    }
}