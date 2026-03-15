using System.Numerics;
using Pamx;
using Pamx.Events;
using Pamx.Keyframes;
using Pamx.Objects;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Shape;

namespace ParallelAnimationSystem.Core.Model;

public static class BeatmapImporter
{
    public static void Import(Beatmap beatmap, BeatmapData beatmapData)
    {
        ParseObjects(beatmap, beatmapData.Objects);
        ParsePrefabInstances(beatmap, beatmapData.PrefabInstances);
        ParsePrefabs(beatmap, beatmapData.Prefabs);
        ParseEvents(beatmap, beatmapData.Events);
        ParseThemes(beatmap, beatmapData.Themes);
        ParseThemeKeyframes(beatmap, beatmapData.Events.Theme);
    }

    private static void ParsePrefabInstances(Beatmap beatmap, IdContainer<BeatmapPrefabInstance> beatmapDataPrefabInstances)
    {
        foreach (var prefabObject in beatmap.PrefabObjects)
        {
            var bmPrefabInstance = new BeatmapPrefabInstance(prefabObject.Id)
            {
                PrefabId = prefabObject.PrefabId,
                StartTime = prefabObject.StartTime,
                Position = prefabObject.Position,
                Scale = prefabObject.Scale,
                Rotation = prefabObject.Rotation
            };
            
            beatmapDataPrefabInstances.Insert(bmPrefabInstance);
        }
    }

    private static void ParsePrefabs(Beatmap beatmap, IdContainer<BeatmapPrefab> beatmapDataPrefabs)
    {
        foreach (var prefab in beatmap.Prefabs)
        {
            var bmPrefab = new BeatmapPrefab(prefab.Id)
            {
                Name = prefab.Name,
                Offset = prefab.Offset
            };
            
            foreach (var obj in prefab.Objects)
                bmPrefab.Objects.Insert(CreateBeatmapObject(obj));
            
            beatmapDataPrefabs.Insert(bmPrefab);
        }
    }

    private static void ParseEvents(Beatmap beatmap, BeatmapEvents events)
    {
        var cameraPositionKeyframes = beatmap.Events.Move
            .Select(positionKeyframe 
                => new Keyframe<Vector2>(positionKeyframe.Time, positionKeyframe.Ease, positionKeyframe.Value));
        events.CameraPosition.Load(cameraPositionKeyframes);

        var cameraScaleKeyframes = beatmap.Events.Zoom
            .Select(scaleKeyframe
                => new Keyframe<float>(scaleKeyframe.Time, scaleKeyframe.Ease, scaleKeyframe.Value));
        events.CameraScale.Load(cameraScaleKeyframes);

        var cameraRotationKeyframes = beatmap.Events.Rotate
            .Select(rotationKeyframe
                => new Keyframe<float>(rotationKeyframe.Time, rotationKeyframe.Ease, rotationKeyframe.Value));
        events.CameraRotation.Load(cameraRotationKeyframes);

        var cameraShakeKeyframes = beatmap.Events.Shake
            .Select(shakeKeyframe => new Keyframe<float>(shakeKeyframe.Time, shakeKeyframe.Ease, shakeKeyframe.Value));
        events.CameraShake.Load(cameraShakeKeyframes);

        var chromaKeyframes = beatmap.Events.Chromatic
            .Select(chromaKeyframe
                => new Keyframe<float>(chromaKeyframe.Time, chromaKeyframe.Ease, chromaKeyframe.Value));
        events.Chroma.Load(chromaKeyframes);

        var bloomKeyframes = beatmap.Events.Bloom
            .Select(bloomKeyframe
                => new Keyframe<BloomValue>(bloomKeyframe.Time, bloomKeyframe.Ease, new BloomValue
                {
                    Intensity = bloomKeyframe.Value.Intensity,
                    Diffusion = bloomKeyframe.Value.Diffusion,
                    Color = bloomKeyframe.Value.Color
                }));
        events.Bloom.Load(bloomKeyframes);

        var vignetteKeyframes = beatmap.Events.Vignette
            .Select(vignetteKeyframe 
                => new Keyframe<VignetteValue>(vignetteKeyframe.Time, vignetteKeyframe.Ease, new VignetteValue
                {
                    Intensity = vignetteKeyframe.Value.Intensity,
                    Smoothness = vignetteKeyframe.Value.Smoothness,
                    IsRounded = vignetteKeyframe.Value.IsRounded,
                    Roundness = vignetteKeyframe.Value.Roundness,
                    Color = vignetteKeyframe.Value.Color,
                    Center = vignetteKeyframe.Value.Center
                }));
        events.Vignette.Load(vignetteKeyframes);
        
        var lensDistortionKeyframes = beatmap.Events.LensDistortion
            .Select(lensDistortionKeyframe 
                => new Keyframe<LensDistortionValue>(lensDistortionKeyframe.Time, lensDistortionKeyframe.Ease, new LensDistortionValue
                {
                    Intensity = lensDistortionKeyframe.Value.Intensity,
                    Center = lensDistortionKeyframe.Value.Center
                }));
        events.LensDistortion.Load(lensDistortionKeyframes);
        
        var grainKeyframes = beatmap.Events.Grain
            .Select(grainKeyframe 
                => new Keyframe<GrainValue>(grainKeyframe.Time, grainKeyframe.Ease, new GrainValue
                {
                    Intensity = grainKeyframe.Value.Intensity,
                    Size = grainKeyframe.Value.Size,
                    Mix = grainKeyframe.Value.Mix,
                    IsColored = grainKeyframe.Value.IsColored
                }));
        events.Grain.Load(grainKeyframes);
        
        var gradientKeyframes = beatmap.Events.Gradient
            .Select(gradientKeyframe 
                => new Keyframe<GradientValue>(gradientKeyframe.Time, gradientKeyframe.Ease, new GradientValue
                {
                    Intensity = gradientKeyframe.Value.Intensity,
                    Rotation = gradientKeyframe.Value.Rotation,
                    StartColor = gradientKeyframe.Value.StartColor,
                    EndColor = gradientKeyframe.Value.EndColor,
                    Mode = gradientKeyframe.Value.Mode
                }));
        events.Gradient.Load(gradientKeyframes);
        
        var glitchKeyframes = beatmap.Events.Glitch
            .Select(glitchKeyframe 
                => new Keyframe<GlitchValue>(glitchKeyframe.Time, glitchKeyframe.Ease, new GlitchValue
                {
                    Intensity = glitchKeyframe.Value.Intensity,
                    Speed = glitchKeyframe.Value.Speed,
                    Width = glitchKeyframe.Value.Width
                }));
        events.Glitch.Load(glitchKeyframes);
        
        var hueKeyframes = beatmap.Events.Hue
            .Select(hueKeyframe 
                => new Keyframe<float>(hueKeyframe.Time, hueKeyframe.Ease, hueKeyframe.Value));
        events.Hue.Load(hueKeyframes);
    }

    private static void ParseThemeKeyframes(Beatmap beatmap, KeyframeList<Keyframe<string>> eventsTheme)
    {
        var themeKeyframes = new List<Keyframe<string>>();
        foreach (var themeKeyframe in beatmap.Events.Theme)
            themeKeyframes.Add(new Keyframe<string>(themeKeyframe.Time, themeKeyframe.Ease, themeKeyframe.Value));
        eventsTheme.Load(themeKeyframes);
    }

    private static void ParseThemes(Beatmap beatmap, IdContainer<BeatmapTheme> beatmapThemes)
    {
        foreach (var theme in beatmap.Themes)
        {
            var bmTheme = new BeatmapTheme(theme.Id)
            {
                Name = theme.Name,
                BackgroundColor = new ColorRgb(theme.Background.R, theme.Background.G, theme.Background.B),
                GuiColor = new ColorRgb(theme.Gui.R, theme.Gui.G, theme.Gui.B),
                GuiAccentColor = new ColorRgb(theme.GuiAccent.R, theme.GuiAccent.G, theme.GuiAccent.B),
            };
            
            for (var i = 0; i < Math.Min(4, theme.Players.Count); i++)
                bmTheme.PlayerColors[i] = new ColorRgb(theme.Players[i].R, theme.Players[i].G, theme.Players[i].B);
            
            for (var i = 0; i < Math.Min(9, theme.Objects.Count); i++)
                bmTheme.ObjectColors[i] = new ColorRgb(theme.Objects[i].R, theme.Objects[i].G, theme.Objects[i].B);
            
            for (var i = 0; i < Math.Min(9, theme.Effects.Count); i++)
                bmTheme.EffectColors[i] = new ColorRgb(theme.Effects[i].R, theme.Effects[i].G, theme.Effects[i].B);
            
            for (var i = 0; i < Math.Min(9, theme.ParallaxObjects.Count); i++)
                bmTheme.ParallaxObjectColors[i] = new ColorRgb(theme.ParallaxObjects[i].R, theme.ParallaxObjects[i].G, theme.ParallaxObjects[i].B);
            
            beatmapThemes.Insert(bmTheme);
        }
    }

    private static void ParseObjects(Beatmap beatmap, IdContainer<BeatmapObject> beatmapObjects)
    {
        foreach (var obj in beatmap.Objects)
            beatmapObjects.Insert(CreateBeatmapObject(obj));
    }

    private static BeatmapObject CreateBeatmapObject(Pamx.Objects.BeatmapObject obj)
    {
        var bmObj = new BeatmapObject(obj.Id)
        {
            Name = obj.Name,
            ParentId = obj.ParentId,
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
            RenderType = obj.Gradient.Type,
            GradientRotation = obj.Gradient.Rotation,
            GradientScale = obj.Gradient.Scale,
            Origin = obj.Origin,
            RenderDepth = obj.RenderDepth,
            StartTime = obj.StartTime,
            AutoKillType = obj.AutoKillType,
            AutoKillOffset = obj.AutoKillOffset,
            Shape = obj.Shape,
            Text = obj.Text,
            CustomShapeInfo = obj.CustomShapeParams is not null &&
                              obj.Shape is ObjectShape.SquareCustom
                                  or ObjectShape.CircleCustom
                                  or ObjectShape.TriangleCustom
                                  or ObjectShape.Custom
                                  or ObjectShape.HexagonCustom
                ? new VGShapeInfo(
                    obj.CustomShapeParams.Sides,
                    obj.CustomShapeParams.Roundness,
                    obj.CustomShapeParams.Thickness,
                    obj.CustomShapeParams.Slices)
                : null
        };

        var positionKeyframes = obj.PositionEvents.Select(CreateVector2Keyframe);
        bmObj.PositionKeyframes.Load(positionKeyframes);

        var scaleKeyframes = obj.ScaleEvents.Select(CreateVector2Keyframe);
        bmObj.ScaleKeyframes.Load(scaleKeyframes);

        var rotationKeyframes = obj.RotationEvents.Select(CreateRotationKeyframe);
        bmObj.RotationKeyframes.Load(rotationKeyframes);

        var colorKeyframes = obj.ColorEvents.Select(CreateColorKeyframe);
        bmObj.ColorKeyframes.Load(colorKeyframes);
        
        return bmObj;
    }

    private static RandomizableKeyframe<Vector2> CreateVector2Keyframe(RandomKeyframe<Vector2> keyframe)
        => new(
            keyframe.Time, keyframe.Ease, keyframe.Value, 
            keyframe.RandomMode, keyframe.RandomValue, keyframe.RandomInterval, 
            false);

    private static RandomizableKeyframe<float> CreateRotationKeyframe(ObjectRotationKeyframe keyframe)
        => new(
            keyframe.Time, keyframe.Ease, keyframe.Value,
            keyframe.RandomMode, keyframe.RandomValue, keyframe.RandomInterval,
            !keyframe.IsAbsolute);

    private static Keyframe<BeatmapObjectIndexedColor> CreateColorKeyframe(FixedKeyframe<ObjectColorValue> keyframe)
        => new(
            keyframe.Time, keyframe.Ease, new BeatmapObjectIndexedColor
            {
                ColorIndex1 = keyframe.Value.Index,
                ColorIndex2 = keyframe.Value.EndIndex,
                Opacity = keyframe.Value.Opacity
            });
}