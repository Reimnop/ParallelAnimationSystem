using System.Numerics;
using Pamx.Common;
using Pamx.Common.Data;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Model;

public static class BeatmapLoader
{
    public static BeatmapData Load(IBeatmap beatmap)
    {
        var beatmapData = new BeatmapData();
        LoadObjects(beatmap, beatmapData.Objects);
        LoadPrefabInstances(beatmap, beatmapData.PrefabInstances);
        LoadPrefabs(beatmap, beatmapData.Prefabs);
        LoadEvents(beatmap, beatmapData.Events);
        LoadThemes(beatmap, beatmapData.Themes);
        LoadThemeKeyframes(beatmap, beatmapData.Events.Theme);
        return beatmapData;
    }

    private static void LoadPrefabInstances(IBeatmap beatmap, IdContainer<BeatmapPrefabInstance> beatmapDataPrefabInstances)
    {
        foreach (var prefabObject in beatmap.PrefabObjects)
        {
            var id = ((IIdentifiable<string>)prefabObject).Id;

            var bmPrefabInstance = new BeatmapPrefabInstance(id)
            {
                PrefabId = prefabObject.Prefab is IIdentifiable<string> prefabIdentifiable ? prefabIdentifiable.Id : null,
                StartTime = prefabObject.Time,
                Position = prefabObject.Position,
                Scale = prefabObject.Scale,
                Rotation = prefabObject.Rotation
            };
            
            beatmapDataPrefabInstances.Insert(bmPrefabInstance);
        }
    }

    private static void LoadPrefabs(IBeatmap beatmap, IdContainer<BeatmapPrefab> beatmapDataPrefabs)
    {
        foreach (var prefab in beatmap.Prefabs)
        {
            var id = ((IIdentifiable<string>)prefab).Id;

            var bmPrefab = new BeatmapPrefab(id)
            {
                Name = prefab.Name,
                Offset = prefab.Offset
            };
            
            foreach (var obj in prefab.BeatmapObjects)
                bmPrefab.Objects.Insert(CreateBeatmapObject(obj));
            
            beatmapDataPrefabs.Insert(bmPrefab);
        }
    }

    private static void LoadEvents(IBeatmap beatmap, BeatmapEventList events)
    {
        foreach (var positionKeyframe in beatmap.Events.Movement)
            events.CameraPosition.Add(new EventKeyframe<Vector2>(positionKeyframe.Time, positionKeyframe.Ease, positionKeyframe.Value));
        
        foreach (var scaleKeyframe in beatmap.Events.Zoom)
            events.CameraScale.Add(new EventKeyframe<float>(scaleKeyframe.Time, scaleKeyframe.Ease, scaleKeyframe.Value));
        
        foreach (var rotationKeyframe in beatmap.Events.Rotation)
            events.CameraRotation.Add(new EventKeyframe<float>(rotationKeyframe.Time, rotationKeyframe.Ease, rotationKeyframe.Value));
    }

    private static void LoadThemeKeyframes(IBeatmap beatmap, KeyframeList<EventKeyframe<string>> eventsTheme)
    {
        foreach (var themeKeyframe in beatmap.Events.Theme)
        {
            string themeId;
            if (themeKeyframe.Value is IIdentifiable<string> stringIdentifiable)
                themeId = stringIdentifiable.Id;
            else if (themeKeyframe.Value is IIdentifiable<int> intIdentifiable)
                themeId = intIdentifiable.Id.ToString();
            else
                throw new InvalidOperationException("Theme does not have a valid identifier");
            
            eventsTheme.Add(new EventKeyframe<string>(themeKeyframe.Time, themeKeyframe.Ease, themeId));
        }
    }

    private static void LoadThemes(IBeatmap beatmap, IdContainer<BeatmapTheme> beatmapThemes)
    {
        foreach (var theme in beatmap.Themes)
        {
            string id;
            if (theme is IIdentifiable<string> stringIdentifiable)
                id = stringIdentifiable.Id;
            else if (theme is IIdentifiable<int> intIdentifiable)
                id = intIdentifiable.Id.ToString();
            else
                throw new InvalidOperationException("Theme does not have a valid identifier");

            var bmTheme = new BeatmapTheme(id)
            {
                Name = theme.Name,
                BackgroundColor = new ColorRgb(theme.Background.R, theme.Background.G, theme.Background.B),
                GuiColor = new ColorRgb(theme.Gui.R, theme.Gui.G, theme.Gui.B),
                GuiAccentColor = new ColorRgb(theme.GuiAccent.R, theme.GuiAccent.G, theme.GuiAccent.B),
            };
            
            for (var i = 0; i < Math.Min(4, theme.Player.Count); i++)
                bmTheme.PlayerColors[i] = new ColorRgb(theme.Player[i].R, theme.Player[i].G, theme.Player[i].B);
            
            for (var i = 0; i < Math.Min(9, theme.Object.Count); i++)
                bmTheme.ObjectColors[i] = new ColorRgb(theme.Object[i].R, theme.Object[i].G, theme.Object[i].B);
            
            for (var i = 0; i < Math.Min(9, theme.Effect.Count); i++)
                bmTheme.EffectColors[i] = new ColorRgb(theme.Effect[i].R, theme.Effect[i].G, theme.Effect[i].B);
            
            for (var i = 0; i < Math.Min(9, theme.ParallaxObject.Count); i++)
                bmTheme.ParallaxObjectColors[i] = new ColorRgb(theme.ParallaxObject[i].R, theme.ParallaxObject[i].G, theme.ParallaxObject[i].B);
            
            beatmapThemes.Insert(bmTheme);
        }
    }

    private static void LoadObjects(IBeatmap beatmap, IdContainer<BeatmapObject> beatmapObjects)
    {
        foreach (var obj in beatmap.Objects)
            beatmapObjects.Insert(CreateBeatmapObject(obj));
    }

    private static BeatmapObject CreateBeatmapObject(IObject obj)
    {
        var id = ((IIdentifiable<string>)obj).Id;

        var bmObj = new BeatmapObject(id)
        {
            Name = obj.Name,
            ParentId = obj.Parent is IIdentifiable<string> parentIdentifiable ? parentIdentifiable.Id : null,
            Type = obj.Type switch
            {
                ObjectType.LegacyNormal => BeatmapObjectType.Hit,
                ObjectType.LegacyHelper => BeatmapObjectType.NoHit,
                ObjectType.LegacyDecoration => BeatmapObjectType.NoHit,
                ObjectType.LegacyEmpty => BeatmapObjectType.Empty,
                ObjectType.Hit => BeatmapObjectType.Hit,
                ObjectType.NoHit => BeatmapObjectType.NoHit,
                _ => BeatmapObjectType.Empty
            },
            ParentType = obj.ParentType,
            ParentOffset = obj.ParentOffset,
            RenderType = obj.RenderType,
            Origin = obj.Origin,
            RenderDepth = obj.RenderDepth,
            StartTime = obj.StartTime,
            AutoKillType = obj.AutoKillType,
            AutoKillOffset = obj.AutoKillOffset,
            Shape = obj.Shape,
            Text = obj.Text
        };
        
        foreach (var positionKeyframe in obj.PositionEvents)
            bmObj.PositionKeyframes.Add(CreateVector2Keyframe(positionKeyframe));

        foreach (var scaleKeyframe in obj.ScaleEvents)
            bmObj.ScaleKeyframes.Add(CreateVector2Keyframe(scaleKeyframe));
        
        foreach (var rotationKeyframe in obj.RotationEvents)
            bmObj.RotationKeyframes.Add(CreateRotationKeyframe(rotationKeyframe));
        
        foreach (var colorKeyframe in obj.ColorEvents)
            bmObj.ColorKeyframes.Add(CreateColorKeyframe(colorKeyframe));
        
        return bmObj;
    }

    private static Vector2Keyframe CreateVector2Keyframe(Keyframe<Vector2> keyframe)
        => new()
        {
            Time = keyframe.Time,
            Ease = keyframe.Ease,
            Value = keyframe.Value,
            RandomMode = keyframe.RandomMode,
            RandomValue = keyframe.RandomValue,
            RandomInterval = keyframe.RandomInterval
        };
    
    private static RotationKeyframe CreateRotationKeyframe(Keyframe<float> keyframe)
        => new()
        {
            Time = keyframe.Time,
            Ease = keyframe.Ease,
            Value = keyframe.Value,
            RandomMode = keyframe.RandomMode,
            RandomValue = keyframe.RandomValue,
            RandomInterval = keyframe.RandomInterval,
            IsRelative = true
        };
    
    private static BeatmapObjectColorKeyframe CreateColorKeyframe(FixedKeyframe<ThemeColor> keyframe)
        => new()
        {
            Time = keyframe.Time,
            Ease = keyframe.Ease,
            Color = new BeatmapObjectIndexedColor
            {
                ColorIndex1 = keyframe.Value.Index,
                ColorIndex2 = keyframe.Value.EndIndex,
                Opacity = keyframe.Value.Opacity
            }
        };
}