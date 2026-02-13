using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Service;

// catering to THEMES *specifically* because they're
// such a needy little bitch...
public class ThemeSequence(PlaybackThemeContainer playbackThemes)
{
    private static readonly ThemeColorState defaultTcs = new();

    private readonly ThemeColorState tcs1 = new();
    private readonly ThemeColorState tcs2 = new();
    private readonly ThemeColorState tcsOut = new();
    
    private readonly List<Keyframe<int>> keyframes = [];
    
    public void LoadKeyframes(IEnumerable<Keyframe<int>> newKeyframes)
    {
        keyframes.Clear();
        keyframes.AddRange(newKeyframes);
        keyframes.Sort((a, b) => a.Time.CompareTo(b.Time));
    }
    
    public ThemeColorState ComputeValueAt(float time)
    {
        if (keyframes.Count == 0)
            return defaultTcs;

        if (time < keyframes[0].Time)
            return ResolveTheme(tcsOut, keyframes[0].Value);
        
        if (time >= keyframes[^1].Time)
            return ResolveTheme(tcsOut, keyframes[^1].Value);
        
        SequenceCommon.GetKeyframePair(keyframes, time, out var firstKeyframe, out var secondKeyframe);
        var t = SequenceCommon.GetMixFactor(time, firstKeyframe.Time, secondKeyframe.Time);
        var easeFunction = EaseFunctions.GetOrLinear(secondKeyframe.Ease);
        
        var firstTcs = ResolveTheme(tcs1, firstKeyframe.Value);
        var secondTcs = ResolveTheme(tcs2, secondKeyframe.Value);
        InterpolateTcs(tcsOut, firstTcs, secondTcs, easeFunction(t));
        return tcsOut;
    }

    private ThemeColorState ResolveTheme(ThemeColorState tcs, int themeIndex)
    {
        if (!playbackThemes.TryGetItem(themeIndex, out var theme))
            return defaultTcs;
        
        tcs.Background = theme.BackgroundColor;
        tcs.Gui = theme.GuiColor;
        tcs.GuiAccent = theme.GuiAccentColor;
        
        for (var i = 0; i < 4; i++)
            tcs.Player[i] = theme.PlayerColors[i];
        
        for (var i = 0; i < 9; i++)
            tcs.Object[i] = theme.ObjectColors[i];
        
        for (var i = 0; i < 9; i++)
            tcs.Effect[i] = theme.EffectColors[i];
        
        for (var i = 0; i < 9; i++)
            tcs.ParallaxObject[i] = theme.ParallaxObjectColors[i];

        return tcs;
    }

    private static void InterpolateTcs(ThemeColorState tcsOut, ThemeColorState tcs1, ThemeColorState tcs2, float t)
    {
        tcsOut.Background = ColorRgb.Lerp(tcs1.Background, tcs2.Background, t);
        tcsOut.Gui = ColorRgb.Lerp(tcs1.Gui, tcs2.Gui, t);
        tcsOut.GuiAccent = ColorRgb.Lerp(tcs1.GuiAccent, tcs2.GuiAccent, t);
        
        for (var i = 0; i < 4; i++)
            tcsOut.Player[i] = ColorRgb.Lerp(tcs1.Player[i], tcs2.Player[i], t);

        for (var i = 0; i < 9; i++)
            tcsOut.Object[i] = ColorRgb.Lerp(tcs1.Object[i], tcs2.Object[i], t);
        
        for (var i = 0; i < 9; i++)
            tcsOut.Effect[i] = ColorRgb.Lerp(tcs1.Effect[i], tcs2.Effect[i], t);
        
        for (var i = 0; i < 9; i++)
            tcsOut.ParallaxObject[i] = ColorRgb.Lerp(tcs1.ParallaxObject[i], tcs2.ParallaxObject[i], t);
    }
}