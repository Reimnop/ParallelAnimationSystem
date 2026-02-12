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
        var cameraPositionKeyframes = beatmap.Events.Movement
            .Select(positionKeyframe 
                => new EventKeyframe<Vector2>(positionKeyframe.Time, positionKeyframe.Ease, positionKeyframe.Value));
        events.CameraPosition.Replace(cameraPositionKeyframes);

        var cameraScaleKeyframes = beatmap.Events.Zoom
            .Select(scaleKeyframe
                => new EventKeyframe<float>(scaleKeyframe.Time, scaleKeyframe.Ease, scaleKeyframe.Value));
        events.CameraScale.Replace(cameraScaleKeyframes);

        var cameraRotationKeyframes = beatmap.Events.Rotation
            .Select(rotationKeyframe
                => new EventKeyframe<float>(rotationKeyframe.Time, rotationKeyframe.Ease, rotationKeyframe.Value));
        events.CameraRotation.Replace(cameraRotationKeyframes);

        var cameraShakeKeyframes = beatmap.Events.Shake
            .Select(shakeKeyframe => new EventKeyframe<float>(shakeKeyframe.Time, shakeKeyframe.Ease, shakeKeyframe.Value));
        events.CameraShake.Replace(cameraShakeKeyframes);

        var chromaKeyframes = beatmap.Events.Chroma
            .Select(chromaKeyframe
                => new EventKeyframe<float>(chromaKeyframe.Time, chromaKeyframe.Ease, chromaKeyframe.Value));
        events.Chroma.Replace(chromaKeyframes);

        var bloomKeyframes = beatmap.Events.Bloom
            .Select(bloomKeyframe
                => new EventKeyframe<BloomData>(bloomKeyframe.Time, bloomKeyframe.Ease, new BloomData
                {
                    Intensity = bloomKeyframe.Value.Intensity,
                    Diffusion = bloomKeyframe.Value.Diffusion,
                    Color = bloomKeyframe.Value.Color
                }));
        events.Bloom.Replace(bloomKeyframes);

        var vignetteKeyframes = beatmap.Events.Vignette
            .Select(vignetteKeyframe 
                => new EventKeyframe<VignetteData>(vignetteKeyframe.Time, vignetteKeyframe.Ease, new VignetteData
                {
                    Intensity = vignetteKeyframe.Value.Intensity,
                    Smoothness = vignetteKeyframe.Value.Smoothness,
                    Rounded = vignetteKeyframe.Value.Rounded,
                    Roundness = vignetteKeyframe.Value.Roundness,
                    Color = vignetteKeyframe.Value.Color,
                    Center = vignetteKeyframe.Value.Center
                }));
        events.Vignette.Replace(vignetteKeyframes);
        
        var lensDistortionKeyframes = beatmap.Events.LensDistortion
            .Select(lensDistortionKeyframe 
                => new EventKeyframe<LensDistortionData>(lensDistortionKeyframe.Time, lensDistortionKeyframe.Ease, new LensDistortionData
                {
                    Intensity = lensDistortionKeyframe.Value.Intensity,
                    Center = lensDistortionKeyframe.Value.Center
                }));
        events.LensDistortion.Replace(lensDistortionKeyframes);
        
        var grainKeyframes = beatmap.Events.Grain
            .Select(grainKeyframe 
                => new EventKeyframe<GrainData>(grainKeyframe.Time, grainKeyframe.Ease, new GrainData
                {
                    Intensity = grainKeyframe.Value.Intensity,
                    Size = grainKeyframe.Value.Size,
                    Mix = grainKeyframe.Value.Mix,
                    Colored = grainKeyframe.Value.Colored
                }));
        events.Grain.Replace(grainKeyframes);
        
        var gradientKeyframes = beatmap.Events.Gradient
            .Select(gradientKeyframe 
                => new EventKeyframe<GradientData>(gradientKeyframe.Time, gradientKeyframe.Ease, new GradientData
                {
                    Intensity = gradientKeyframe.Value.Intensity,
                    Rotation = gradientKeyframe.Value.Rotation,
                    ColorA = gradientKeyframe.Value.ColorA,
                    ColorB = gradientKeyframe.Value.ColorB,
                    Mode = gradientKeyframe.Value.Mode
                }));
        events.Gradient.Replace(gradientKeyframes);
        
        var glitchKeyframes = beatmap.Events.Glitch
            .Select(glitchKeyframe 
                => new EventKeyframe<GlitchData>(glitchKeyframe.Time, glitchKeyframe.Ease, new GlitchData
                {
                    Intensity = glitchKeyframe.Value.Intensity,
                    Speed = glitchKeyframe.Value.Speed,
                    Width = glitchKeyframe.Value.Width
                }));
        events.Glitch.Replace(glitchKeyframes);
        
        var hueKeyframes = beatmap.Events.Hue
            .Select(hueKeyframe 
                => new EventKeyframe<float>(hueKeyframe.Time, hueKeyframe.Ease, hueKeyframe.Value));
        events.Hue.Replace(hueKeyframes);
    }

    private static void LoadThemeKeyframes(IBeatmap beatmap, KeyframeList<EventKeyframe<string>> eventsTheme)
    {
        var themeKeyframes = new List<EventKeyframe<string>>();
        foreach (var themeKeyframe in beatmap.Events.Theme)
        {
            string themeId;
            if (themeKeyframe.Value is IIdentifiable<string> stringIdentifiable)
                themeId = stringIdentifiable.Id;
            else if (themeKeyframe.Value is IIdentifiable<int> intIdentifiable)
                themeId = intIdentifiable.Id.ToString();
            else
                throw new InvalidOperationException("Theme does not have a valid identifier");
            
            themeKeyframes.Add(new EventKeyframe<string>(themeKeyframe.Time, themeKeyframe.Ease, themeId));
        }
        eventsTheme.Replace(themeKeyframes);
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

        var positionKeyframes = obj.PositionEvents.Select(CreateVector2Keyframe);
        bmObj.PositionKeyframes.Replace(positionKeyframes);

        var scaleKeyframes = obj.ScaleEvents.Select(CreateVector2Keyframe);
        bmObj.ScaleKeyframes.Replace(scaleKeyframes);

        var rotationKeyframes = obj.RotationEvents.Select(CreateRotationKeyframe);
        bmObj.RotationKeyframes.Replace(rotationKeyframes);

        var colorKeyframes = obj.ColorEvents.Select(CreateColorKeyframe);
        bmObj.ColorKeyframes.Replace(colorKeyframes);
        
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