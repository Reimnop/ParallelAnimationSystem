using System.Drawing;
using System.Text.Json.Nodes;
using Pamx.Common;
using Pamx.Common.Data;
using Pamx.Common.Enum;
using Pamx.Common.Implementation;
using Pamx.Ls;

namespace ParallelAnimationSystem.Core;

public class LsMigration(ResourceLoader loader)
{
    public void MigrateBeatmap(IBeatmap beatmap)
    {
        var themes = LoadThemes();
        
        // Add to beatmap
        beatmap.Themes.AddRange(themes.Values);
        
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
        foreach (var o in beatmap.Objects.Concat(beatmap.Prefabs.SelectMany(x => x.BeatmapObjects)))
        {
            if (o.Type == ObjectType.LegacyHelper)
            {
                // Replace all the color events
                for (var i = 0; i < o.ColorEvents.Count; i++)
                {
                    var oldColorKeyframe = o.ColorEvents[i];
                    oldColorKeyframe.Value = new ThemeColor
                    {
                        Index = oldColorKeyframe.Value.Index,
                        EndIndex = oldColorKeyframe.Value.EndIndex,
                        Opacity = 0.35f,
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
        
        // Migrate bloom keyframes
        for (var i = 0; i < events.Bloom.Count; i++)
        {
            var bloomKeyframe = events.Bloom[i];
            bloomKeyframe.Value = new BloomData
            {
                Intensity = bloomKeyframe.Value.Intensity,
                Diffusion = 24.0f,
            };
            events.Bloom[i] = bloomKeyframe;
        }
        
        // Migrate chromatic aberration keyframes
        for (var i = 0; i < events.Chroma.Count; i++)
        {
            var chromaticAberrationKeyframe = events.Chroma[i];
            chromaticAberrationKeyframe.Value /= 40.0f;
            events.Chroma[i] = chromaticAberrationKeyframe;
        }
        
        // Migrate theme keyframes
        for (var i = 0; i < events.Theme.Count; i++)
        {
            var themeKeyframe = events.Theme[i];
            if (themeKeyframe.Value is IIdentifiable<int> themeIdentifiable &&
                themes.TryGetValue(themeIdentifiable.Id, out var theme))
                events.Theme[i] = new FixedKeyframe<IReference<ITheme>>
                {
                    Time = themeKeyframe.Time,
                    Value = theme,
                    Ease = themeKeyframe.Ease,
                };
        }
    }
    
    private Dictionary<int, ITheme> LoadThemes()
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
        
        var themes = new Dictionary<int, ITheme>();

        foreach (var themeName in themeNames)
        {
            var themeData = loader.ReadResourceString($"Themes/{themeName}");
            if (themeData is null)
                throw new InvalidOperationException($"Could not load theme data for '{themeName}'");
            
            var json = JsonNode.Parse(themeData);
            if (json is not JsonObject jsonObject)
                continue;
            var theme = (LsBeatmapTheme) LsDeserialization.DeserializeTheme(jsonObject);
            themes.Add(theme.Id, theme);
        }
        
        return themes;
    }

    private void FixTheme(ITheme theme)
    {
        // Make sure theme has correct amount of colors
        for (var i = theme.Player.Count; i < 4; i++)
        {
            theme.Player.Add(Color.Black);
        }
        
        for (var i = theme.Object.Count; i < 9; i++)
        {
            theme.Object.Add(Color.Black);
        }
        
        for (var i = theme.BackgroundObject.Count; i < 9; i++)
        {
            theme.BackgroundObject.Add(Color.Black);
        }
    }
}