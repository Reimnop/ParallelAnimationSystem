using System.Drawing;
using System.Text.Json;
using Pamx;
using Pamx.Keyframes;
using Pamx.Objects;
using Pamx.Serialization;
using Pamx.Themes;

namespace ParallelAnimationSystem.Core;

public class LsMigration(ResourceLoader loader)
{
    public void MigrateBeatmap(Beatmap beatmap)
    {
        var themes = LoadThemes();
        
        // Add to beatmap
        beatmap.Themes.AddRange(themes);
        
        // Fix themes
        foreach (var theme in beatmap.Themes)
            FixTheme(theme);
        
        // Negate prefab offsets
        foreach (var prefab in beatmap.Prefabs)
        {
            prefab.Offset = -prefab.Offset;
        }

        // Set default scale to 1 if scale is 0
        foreach (var prefabObject in beatmap.PrefabObjects)
        {
            if (prefabObject.Scale == System.Numerics.Vector2.Zero)
                prefabObject.Scale = System.Numerics.Vector2.One;
        }

        // Migrate theme color keyframes
        foreach (var o in beatmap.Objects.Concat(beatmap.Prefabs.SelectMany(x => x.Objects)))
        {
            if (o.Type == ObjectType.LegacyHelper)
            {
                // Replace all the color events
                for (var i = 0; i < o.ColorEvents.Count; i++)
                {
                    var oldColorKeyframe = o.ColorEvents[i];
                    oldColorKeyframe.Value = new ObjectColorValue
                    {
                        Index = oldColorKeyframe.Value.Index,
                        EndIndex = oldColorKeyframe.Value.EndIndex,
                        Opacity = 0.35f
                    };
                    o.ColorEvents[i] = oldColorKeyframe;
                }
            }

            if (o.Shape == ObjectShape.Text)
            {
                // Since legacy uses a different font, we need to change the font here
                o.Text = $"<font=\"Inconsolata SDF\">{o.Text}";
            }
        }
        
        var events = beatmap.Events;
        
        // Migrate chromatic aberration keyframes
        for (var i = 0; i < events.Chromatic.Count; i++)
        {
            var chromaticAberrationKeyframe = events.Chromatic[i];
            chromaticAberrationKeyframe.Value /= 40.0f;
            events.Chromatic[i] = chromaticAberrationKeyframe;
        }
        
        // Migrate theme keyframes
        for (var i = 0; i < events.Theme.Count; i++)
        {
            var themeKeyframe = events.Theme[i];
            events.Theme[i] = new FixedKeyframe<string>
            {
                Time = themeKeyframe.Time,
                Value = themeKeyframe.Value,
                Ease = themeKeyframe.Ease,
            };
        }
    }
    
    private List<BeatmapTheme> LoadThemes()
    {
        var themeNames = new[]
        {
            "Anarchy.lst",
            "BlackWhite.lst",
            "Classic.lst",
            "Dark.lst",
            "DayNight.lst",
            "Donuts.lst",
            "Machine.lst",
            "New.lst",
            "WhiteBlack.lst"
        };

        var themes = new List<BeatmapTheme>();
        
        foreach (var themeName in themeNames)
        {
            var themeData = loader.ReadResourceString($"Themes/{themeName}");
            if (themeData is null)
                throw new InvalidOperationException($"Could not load theme data for '{themeName}'");

            var theme = JsonSerializer.Deserialize<BeatmapTheme>(themeData, PamxSerialization.LegacyOptions)!;
            themes.Add(theme);
        }

        return themes;
    }

    private void FixTheme(ExternalTheme theme)
    {
        for (var i = theme.Players.Count; i < 4; i++)
        {
            theme.Players.Add(Color.Black);
        }
        
        for (var i = theme.Objects.Count; i < 9; i++)
        {
            theme.Objects.Add(Color.Black);
        }
        
        for (var i = theme.BackgroundObjects.Count; i < 9; i++)
        {
            theme.BackgroundObjects.Add(Color.Black);
        }
    }
}