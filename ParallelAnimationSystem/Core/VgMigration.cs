using System.Drawing;
using System.Text.Json.Nodes;
using Pamx.Common;
using Pamx.Common.Data;
using Pamx.Common.Enum;
using Pamx.Common.Implementation;
using Pamx.Vg;
using ParallelAnimationSystem.Data;

namespace ParallelAnimationSystem.Core;

public static class VgMigration
{
    public static void MigrateBeatmap(IBeatmap beatmap, IResourceManager resourceManager)
    {
        // Load themes
        var themes = LoadThemes(resourceManager);
        
        // Add to beatmap
        beatmap.Themes.AddRange(themes.Values);
        
        // Fix themes
        foreach (var theme in beatmap.Themes)
            FixTheme(theme);
        
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
        }
        
        var events = beatmap.Events;
        
        // Migrate theme keyframes
        for (var i = 0; i < events.Theme.Count; i++)
        {
            var themeKeyframe = events.Theme[i];
            if (themeKeyframe.Value is IIdentifiable<string> themeIdentifiable &&
                themes.TryGetValue(themeIdentifiable.Id, out var theme))
                events.Theme[i] = new FixedKeyframe<IReference<ITheme>>
                {
                    Time = themeKeyframe.Time,
                    Value = theme,
                    Ease = themeKeyframe.Ease,
                };
        }
        
        // We'll handle camera parenting at migration
        // Start by inserting an empty camera object at the start of the beatmap
        var cameraObject = new BeatmapObject
        {
            StartTime = 0.0f,
            AutoKillType = AutoKillType.NoAutoKill,
            Type = ObjectType.Empty,
            PositionEvents = beatmap.Events.Movement.Select(x => new Keyframe<System.Numerics.Vector2>
            {
                Value = x.Value,
                Time = x.Time,
                Ease = x.Ease,
            }).ToList(),
            ScaleEvents = beatmap.Events.Zoom.Select(x => new Keyframe<System.Numerics.Vector2>
            {
                Value = x.Value / 20.0f * System.Numerics.Vector2.One,
                Time = x.Time,
                Ease = x.Ease,
            }).ToList(),
            // We'll handle rotation separately because camera rotation is absolute rotation
        };
        
        var lastCameraRotation = 0.0f;
        foreach (var rotationEvent in beatmap.Events.Rotation)
        {
            var newRotationEvent = new Keyframe<float>
            {
                Value = rotationEvent.Value - lastCameraRotation,
                Time = rotationEvent.Time,
                Ease = rotationEvent.Ease,
            };
            lastCameraRotation = rotationEvent.Value;
            cameraObject.RotationEvents.Add(newRotationEvent);
        }
        
        // Insert the camera object
        beatmap.Objects.Add(cameraObject);
        
        // Now we'll parent all the camera parent objects to the camera object
        foreach (var o in beatmap.Objects.Concat(beatmap.Prefabs.SelectMany(x => x.BeatmapObjects)))
        {
            if (o.Parent is IIdentifiable<string> { Id: "camera" })
            {
                o.Parent = cameraObject;
                o.ParentType = ParentType.Position | ParentType.Scale | ParentType.Rotation;
                
                // Make sure camera parented objects are above everything else
                o.RenderDepth -= 100;
            }
        }
    }

    private static Dictionary<string, ITheme> LoadThemes(IResourceManager resourceManager)
    {
        var themeNames = new[]
        {
            "Anarchy.vgt",
            "BlackWhite.vgt",
            "Classic.vgt",
            "Dark.vgt",
            "DayNight.vgt",
            "DesertHeat.vgt",
            "Donuts.vgt",
            "EmberStones.vgt",
            "FireArmour.vgt",
            "HotPanda.vgt",
            "JungleWaterway.vgt",
            "Lure.vgt",
            "Machine.vgt",
            "New.vgt",
            "Poison.vgt",
            "Shiver.vgt",
            "Starlight.vgt",
            "StoneField.vgt",
            "ViciousGoop.vgt",
            "WhiteBlack.vgt",
            "Wonderland.vgt",
        };
        
        var themes = new Dictionary<string, ITheme>();
        
        foreach (var themeName in themeNames)
        {
            var themeString = resourceManager.LoadResourceString($"Themes/{themeName}");
            var json = JsonNode.Parse(themeString);
            if (json is not JsonObject jsonObject)
                continue;
            
            var theme = VgDeserialization.DeserializeTheme(jsonObject);
            themes.Add(themeName, theme);
        }
        
        return themes;
    }
    
    private static void FixTheme(ITheme theme)
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
        
        for (var i = theme.ParallaxObject.Count; i < 9; i++)
        {
            theme.ParallaxObject.Add(Color.Black);
        }
        
        for (var i = theme.Effect.Count; i < 9; i++)
        {
            theme.Effect.Add(Color.Black);
        }
    }
}