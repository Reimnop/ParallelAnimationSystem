using System.IO.Hashing;
using System.Text;
using Microsoft.Extensions.Logging;
using OpenTK.Mathematics;
using Pamx.Common;
using Pamx.Common.Data;
using Pamx.Common.Enum;
using Pamx.Common.Implementation;
using ParallelAnimationSystem.Core.Animation;
using ParallelAnimationSystem.Core.Beatmap;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Util;
using BeatmapObject = ParallelAnimationSystem.Core.Beatmap.BeatmapObject;

namespace ParallelAnimationSystem.Core;

public class BeatmapImporter(ulong randomSeed, ILogger logger)
{
    public AnimationRunner CreateRunner(IBeatmap beatmap)
    {
        // Convert all the objects in the beatmap to Timeline
        var timeline = CreateTimeline(beatmap);

        // Get theme sequence
        var themeColorSequence = CreateThemeSequence(beatmap.Events.Theme);

        // Get camera sequences
        var cameraPositionSequence = CreateCameraPositionSequence(beatmap.Events.Movement);
        var cameraScaleSequence = CreateCameraScaleSequence(beatmap.Events.Zoom);
        var cameraRotationSequence = CreateCameraRotationSequence(beatmap.Events.Rotation);

        // Get post-processing sequences
        var bloomSequence = CreateBloomSequence(beatmap.Events.Bloom);
        var hueSequence = CreateHueSequence(beatmap.Events.Hue);
        var lensDistortionSequence = CreateLensDistortionSequence(beatmap.Events.LensDistortion);
        var chromaticAberrationSequence = CreateChromaticAberrationSequence(beatmap.Events.Chroma);
        var vignetteSequence = CreateVignetteSequence(beatmap.Events.Vignette);
        var gradientSequence = CreateGradientSequence(beatmap.Events.Gradient);
        var glitchSequence = CreateGlitchSequence(beatmap.Events.Glitch);
        var shakeSequence = CreateShakeSequence(beatmap.Events.Shake);

        // Create the runner with the GameObjects
        return new AnimationRunner(
            timeline,
            themeColorSequence,
            cameraPositionSequence,
            cameraScaleSequence,
            cameraRotationSequence,
            bloomSequence,
            hueSequence,
            lensDistortionSequence,
            chromaticAberrationSequence,
            vignetteSequence,
            gradientSequence,
            glitchSequence,
            shakeSequence);
    }

    private Sequence<SequenceKeyframe<float>, object?, float> CreateShakeSequence(IList<FixedKeyframe<float>> shakeEvents)
    {
        var keyframes = shakeEvents
            .Select(x => new SequenceKeyframe<float>
            {
                Time = x.Time,
                Ease = x.Ease,
                Value = x.Value,
            });
        return new Sequence<SequenceKeyframe<float>, object?, float>(keyframes, SequenceKeyframe<float>.ResolveToValue, MathUtil.Lerp);
    }
    
    private Sequence<SequenceKeyframe<GlitchData>, object?, GlitchData> CreateGlitchSequence(IList<FixedKeyframe<GlitchData>> glitchEvents)
    {
        var keyframes = glitchEvents
            .Select(x => new SequenceKeyframe<GlitchData>
            {
                Time = x.Time,
                Ease = x.Ease,
                Value = x.Value,
            });
        return new Sequence<SequenceKeyframe<GlitchData>, object?, GlitchData>(keyframes, (x, _) => x.Value, InterpolateGlitchData);
    }

    private static GlitchData InterpolateGlitchData(GlitchData a, GlitchData b, float t)
        => new()
        {
            Intensity = MathUtil.Lerp(a.Intensity, b.Intensity, t),
            Speed = MathUtil.Lerp(a.Speed, b.Speed, t),
            Width = MathUtil.Lerp(a.Width, b.Width, t)
        };

    private Sequence<SequenceKeyframe<GradientData>, ThemeColorState, GradientEffectState> CreateGradientSequence(IList<FixedKeyframe<GradientData>> gradientEvents)
    {
        var keyframes = gradientEvents
            .Select(x => new SequenceKeyframe<GradientData>
            {
                Time = x.Time,
                Ease = x.Ease,
                Value = x.Value,
            });
        return new Sequence<SequenceKeyframe<GradientData>, ThemeColorState, GradientEffectState>(keyframes, ResolveGradientDataKeyframe, InterpolateGradientEffectState);
    }

    private static GradientEffectState ResolveGradientDataKeyframe(SequenceKeyframe<GradientData> keyframe, ThemeColorState context)
    {
        var value = keyframe.Value;
        var color1Index = Math.Clamp(value.ColorA, 0, context.Effect.Length - 1);
        var color2Index = Math.Clamp(value.ColorB, 0, context.Effect.Length - 1);
        var color1 = context.Effect[color1Index];
        var color2 = context.Effect[color2Index];
        var color1Vector = new Vector3(color1.R, color1.G, color1.B);
        var color2Vector = new Vector3(color2.R, color2.G, color2.B);
        var mode = value.Mode;
        return new GradientEffectState
        {
            Color1 = color1Vector,
            Color2 = color2Vector,
            Intensity = value.Intensity,
            Rotation = value.Rotation,
            Mode = mode
        };
    }
    
    private static GradientEffectState InterpolateGradientEffectState(GradientEffectState a, GradientEffectState b, float t)
    { 
        var color1 = MathUtil.Lerp(a.Color1, b.Color1, t);
        var color2 = MathUtil.Lerp(a.Color2, b.Color2, t);
        var mode = a.Mode;
        
        return new GradientEffectState
        {
            Color1 = color1,
            Color2 = color2,
            Intensity = MathUtil.Lerp(a.Intensity, b.Intensity, t),
            Rotation = MathUtil.Lerp(a.Rotation, b.Rotation, t),
            Mode = mode
        };
    }

    private Sequence<SequenceKeyframe<VignetteData>, ThemeColorState, VignetteEffectState> CreateVignetteSequence(IList<FixedKeyframe<VignetteData>> vignetteEvents)
    {
        var keyframes = vignetteEvents
            .Select(x => new SequenceKeyframe<VignetteData>
            {
                Time = x.Time,
                Ease = x.Ease,
                Value = x.Value,
            });
        return new Sequence<SequenceKeyframe<VignetteData>, ThemeColorState, VignetteEffectState>(keyframes, ResolveVignetteDataKeyframe, InterpolateVignetteEffectState);
    }
    
    private static VignetteEffectState ResolveVignetteDataKeyframe(SequenceKeyframe<VignetteData> keyframe, ThemeColorState context)
    {
        var value = keyframe.Value;
        var color = value.Color.HasValue 
            ? context.Effect[Math.Clamp(value.Color.Value, 0, context.Effect.Length - 1)]
            : new ColorRgb(0.0f, 0.0f, 0.0f);
        var colorVector = new Vector3(color.R, color.G, color.B);
        var rounded = value.Rounded;
        var roundness = value.Roundness ?? 1.0f;
        return new VignetteEffectState
        {
            Intensity = value.Intensity,
            Smoothness = value.Smoothness,
            Color = colorVector,
            Rounded = rounded,
            Roundness = roundness,
            Center = new Vector2(value.Center.X, value.Center.Y),
        };
    }

    private static VignetteEffectState InterpolateVignetteEffectState(VignetteEffectState a, VignetteEffectState b, float t)
        => new()
        {
            Intensity = MathUtil.Lerp(a.Intensity, b.Intensity, t),
            Smoothness = MathUtil.Lerp(a.Smoothness, b.Smoothness, t),
            Color = MathUtil.Lerp(a.Color, b.Color, t),
            Rounded = t > 0.5f ? b.Rounded : a.Rounded,
            Roundness = MathUtil.Lerp(a.Roundness, b.Roundness, t),
            Center = MathUtil.Lerp(a.Center, b.Center, t),
        };

    private Sequence<SequenceKeyframe<float>, object?, float> CreateChromaticAberrationSequence(IList<FixedKeyframe<float>> chromaEvents)
    {
        var keyframes = chromaEvents
            .Select(x => new SequenceKeyframe<float>
            {
                Time = x.Time,
                Ease = x.Ease,
                Value = x.Value,
            });
        return new Sequence<SequenceKeyframe<float>, object?, float>(keyframes, SequenceKeyframe<float>.ResolveToValue, MathUtil.Lerp);
    }

    private Sequence<SequenceKeyframe<LensDistortionData>, object?, LensDistortionData> CreateLensDistortionSequence(IList<FixedKeyframe<LensDistortionData>> lensDistortionEvents)
    {
        var keyframes = lensDistortionEvents
            .Select(x => new SequenceKeyframe<LensDistortionData>
            {
                Time = x.Time,
                Ease = x.Ease,
                Value = x.Value,
            });
        return new Sequence<SequenceKeyframe<LensDistortionData>, object?, LensDistortionData>(
            keyframes,
            SequenceKeyframe<LensDistortionData>.ResolveToValue,
            InterpolateLensDistortionData);
    }
    
    private LensDistortionData InterpolateLensDistortionData(LensDistortionData a, LensDistortionData b, float t)
        => new()
        {
            Intensity = MathUtil.Lerp(a.Intensity, b.Intensity, t),
            Center = new System.Numerics.Vector2(
                MathUtil.Lerp(a.Center.X, b.Center.X, t),
                MathUtil.Lerp(a.Center.Y, b.Center.Y, t))
        };
    
    private Sequence<SequenceKeyframe<BloomData>, ThemeColorState, BloomEffectState> CreateBloomSequence(IList<FixedKeyframe<BloomData>> bloomEvents)
    {
        var keyframes = bloomEvents
            .Select(x => new SequenceKeyframe<BloomData>
            {
                Time = x.Time,
                Ease = x.Ease,
                Value = x.Value,
            });
        return new Sequence<SequenceKeyframe<BloomData>, ThemeColorState, BloomEffectState>(keyframes, ResolveBloomDataKeyframe, InterpolateBloomEffectState);
    }

    private static BloomEffectState ResolveBloomDataKeyframe(SequenceKeyframe<BloomData> keyframe, ThemeColorState context)
    {
        var colorIndex = Math.Clamp(keyframe.Value.Color, 0, context.Effect.Length - 1);
        var color = context.Effect[colorIndex];
        var colorVector = new Vector3(color.R, color.G, color.B);
        return new BloomEffectState
        {
            Intensity = keyframe.Value.Intensity,
            Diffusion = keyframe.Value.Diffusion,
            Color = colorVector,
        };
    }

    private static BloomEffectState InterpolateBloomEffectState(BloomEffectState a, BloomEffectState b, float t)
        => new()
        {
            Intensity = MathUtil.Lerp(a.Intensity, b.Intensity, t),
            Diffusion = MathUtil.Lerp(a.Diffusion, b.Diffusion, t),
            Color = MathUtil.Lerp(a.Color, b.Color, t),
        };

    private Sequence<SequenceKeyframe<float>, object?, float> CreateHueSequence(IList<FixedKeyframe<float>> hueEvents)
    {
        var keyframes = hueEvents
            .Select(x => new SequenceKeyframe<float>
            {
                Time = x.Time,
                Ease = x.Ease,
                Value = MathHelper.DegreesToRadians(x.Value),
            });
        return new Sequence<SequenceKeyframe<float>, object?, float>(keyframes, SequenceKeyframe<float>.ResolveToValue, MathUtil.Lerp);
    }
    
    private Sequence<SequenceKeyframe<Vector2>, object?, Vector2> CreateCameraPositionSequence(IList<FixedKeyframe<System.Numerics.Vector2>> cameraPositionEvents)
    {
        var keyframes = cameraPositionEvents
            .Select(x => new SequenceKeyframe<Vector2>
            {
                Time = x.Time,
                Ease = x.Ease,
                Value = new Vector2(x.Value.X, x.Value.Y),
            });
        return new Sequence<SequenceKeyframe<Vector2>, object?, Vector2>(keyframes, SequenceKeyframe<Vector2>.ResolveToValue, MathUtil.Lerp);
    }
    
    private Sequence<SequenceKeyframe<float>, object?, float> CreateCameraScaleSequence(IList<FixedKeyframe<float>> cameraScaleEvents)
    {
        var keyframes = cameraScaleEvents
            .Select(x => new SequenceKeyframe<float>
            {
                Time = x.Time,
                Ease = x.Ease,
                Value = x.Value,
            });
        return new Sequence<SequenceKeyframe<float>, object?, float>(keyframes, SequenceKeyframe<float>.ResolveToValue, MathUtil.Lerp);
    }
    
    private Sequence<SequenceKeyframe<float>, object?, float> CreateCameraRotationSequence(IList<FixedKeyframe<float>> cameraScaleEvents)
        {
            var keyframes = cameraScaleEvents
                .Select(x => new SequenceKeyframe<float>
                {
                    Time = x.Time,
                    Ease = x.Ease,
                    Value = MathHelper.DegreesToRadians(x.Value),
                });
            return new Sequence<SequenceKeyframe<float>, object?, float>(keyframes, SequenceKeyframe<float>.ResolveToValue, MathUtil.Lerp);
        }

    private Sequence<SequenceKeyframe<ITheme>, object?, ThemeColorState> CreateThemeSequence(IList<FixedKeyframe<IReference<ITheme>>> themeEvents)
    {
        var keyframes = themeEvents
            .Where(x => x.Value.Value is not null)
            .Select(x => new SequenceKeyframe<ITheme>
            {
                Time = x.Time,
                Ease = x.Ease,
                Value = x.Value.Value ?? throw new ArgumentNullException(nameof(x.Value.Value)),
            });
        return new Sequence<SequenceKeyframe<ITheme>, object?, ThemeColorState>(keyframes, ResolveThemeKeyframe, InterpolateThemeColorState);
    }

    private static ThemeColorState ResolveThemeKeyframe(SequenceKeyframe<ITheme> keyframe, object? _)
    {
        var value = keyframe.Value;
        
        var themeColorState = new ThemeColorState
        {
            Background = value.Background.ToColorRgb(),
            Gui = value.Gui.ToColorRgb(),
            GuiAccent = value.GuiAccent.ToColorRgb(),
        };
        
        for (var i = 0; i < Math.Min(value.Player.Count, themeColorState.Player.Length); i++)
            themeColorState.Player[i] = value.Player[i].ToColorRgb();
        for (var i = value.Player.Count; i < themeColorState.Player.Length; i++)
            themeColorState.Player[i] = new ColorRgb(0.0f, 0.0f, 0.0f);
        
        for (var i = 0; i < Math.Min(value.Object.Count, themeColorState.Object.Length); i++)
            themeColorState.Object[i] = value.Object[i].ToColorRgb();
        for (var i = value.Object.Count; i < themeColorState.Object.Length; i++)
            themeColorState.Object[i] = new ColorRgb(0.0f, 0.0f, 0.0f);
        
        for (var i = 0; i < Math.Min(value.Effect.Count, themeColorState.Effect.Length); i++)
            themeColorState.Effect[i] = value.Effect[i].ToColorRgb();
        for (var i = value.Effect.Count; i < themeColorState.Effect.Length; i++)
            themeColorState.Effect[i] = new ColorRgb(0.0f, 0.0f, 0.0f);
        
        for (var i = 0; i < Math.Min(value.ParallaxObject.Count, themeColorState.ParallaxObject.Length); i++)
            themeColorState.ParallaxObject[i] = value.ParallaxObject[i].ToColorRgb();
        for (var i = value.ParallaxObject.Count; i < themeColorState.ParallaxObject.Length; i++)
            themeColorState.ParallaxObject[i] = new ColorRgb(0.0f, 0.0f, 0.0f);
        
        return themeColorState;
    }
    
    private static ThemeColorState InterpolateThemeColorState(ThemeColorState a, ThemeColorState b, float t)
    {
        var themeColorState = new ThemeColorState
        {
            Background = ColorRgb.Lerp(a.Background, b.Background, t),
            Gui = ColorRgb.Lerp(a.Gui, b.Gui, t),
            GuiAccent = ColorRgb.Lerp(a.GuiAccent, b.GuiAccent, t)
        };
        for (var i = 0; i < themeColorState.Player.Length; i++)
            themeColorState.Player[i] = ColorRgb.Lerp(a.Player[i], b.Player[i], t);
        for (var i = 0; i < themeColorState.Object.Length; i++)
            themeColorState.Object[i] = ColorRgb.Lerp(a.Object[i], b.Object[i], t);
        for (var i = 0; i < themeColorState.Effect.Length; i++)
            themeColorState.Effect[i] = ColorRgb.Lerp(a.Effect[i], b.Effect[i], t);
        for (var i = 0; i < themeColorState.ParallaxObject.Length; i++)
            themeColorState.ParallaxObject[i] = ColorRgb.Lerp(a.ParallaxObject[i], b.ParallaxObject[i], t);
        return themeColorState;
    }

    private Timeline CreateTimeline(IBeatmap beatmap)
    {
        var timeline = new Timeline();
        
        var beatmapObjects = timeline.BeatmapObjects;
        foreach (var @object in beatmap.Objects)
        {
            var factory = CreateBeatmapObjectFactory(@object, null, randomSeed, ((IIdentifiable<string>) @object).Id);
            beatmapObjects.Add(factory);
        }
        
        // Connect parent tree relations
        foreach (var obj in beatmap.Objects)
        {
            var childId = ((IIdentifiable<string>) obj).Id;
            var parentId = obj.Parent is IIdentifiable<string> parentIdentifiable 
                ? parentIdentifiable.Id
                : null;
            if (parentId is not null && beatmapObjects.Contains(parentId))
                beatmapObjects.SetParent(childId, parentId);
        }
        
        // Expand prefabs
        foreach (var prefabInstance in beatmap.PrefabObjects)
        {
            var positionKeyframes = new List<PositionScaleKeyframe> 
            {
                new()
                {
                    Time = 0.0f,
                    Ease = Ease.Linear,
                    Value = new Vector2(prefabInstance.Position.X, prefabInstance.Position.Y),
                }
            };
            
            var scaleKeyframes = new List<PositionScaleKeyframe> 
            {
                new()
                {
                    Time = 0.0f,
                    Ease = Ease.Linear,
                    Value = new Vector2(prefabInstance.Scale.X, prefabInstance.Scale.Y),
                }
            };
            
            var rotationKeyframes = new List<RotationKeyframe> 
            {
                new()
                {
                    Time = 0.0f,
                    Ease = Ease.Linear,
                    Value = MathHelper.DegreesToRadians(prefabInstance.Rotation),
                }
            };
        
            var globalPrefabParent = beatmapObjects.Add(numericId =>
            {
                var stringId = RandomUtil.GenerateId();
                return new BeatmapObject(
                    new ObjectId(stringId, numericId),
                    positionKeyframes,
                    scaleKeyframes,
                    rotationKeyframes,
                    [])
                {
                    ParentTypes = new ParentTypes(true, true, true),
                };
            });
            
            var prefab = (IPrefab)prefabInstance.Prefab;
            var prefabObjectIdMap = prefab.BeatmapObjects
                .ToDictionary(x => ((IIdentifiable<string>) x).Id, _ => RandomUtil.GenerateId());
            
            var prefabInstanceId = ((IIdentifiable<string>) prefabInstance).Id;
        
            foreach (var prefabObject in prefab.BeatmapObjects)
            {
                var objectId = ((IIdentifiable<string>) prefabObject).Id;
                var mappedId = prefabObjectIdMap[objectId];
                var factory = CreateBeatmapObjectFactory(prefabObject, mappedId, randomSeed, objectId, prefabInstanceId);
                beatmapObjects.Add(numericId =>
                {
                    var beatmapObject = factory(numericId);
                    beatmapObject.StartTime += prefabInstance.Time - prefab.Offset;
                    return beatmapObject;
                });
            }
            
            // Resolve parenting
            foreach (var prefabObject in prefab.BeatmapObjects)
            {
                var childId = ((IIdentifiable<string>) prefabObject).Id;
                var mappedChildId = prefabObjectIdMap[childId];
                
                var parentId = prefabObject.Parent is IIdentifiable<string> parentIdentifiable 
                    ? parentIdentifiable.Id
                    : null;
        
                if (parentId is not null)
                {
                    // Check if object is in prefab
                    if (prefabObjectIdMap.TryGetValue(parentId, out var mappedParentId))
                    {
                        beatmapObjects.SetParent(mappedChildId, mappedParentId);
                    }
                    // If object is not in prefab, check if object is in beatmap
                    else if (beatmapObjects.Contains(parentId))
                    {
                        var specificObjectParent = beatmapObjects.Add(numericId =>
                        {
                            var stringId = RandomUtil.GenerateId();
                            return new BeatmapObject(
                                new ObjectId(stringId, numericId),
                                positionKeyframes,
                                scaleKeyframes,
                                rotationKeyframes,
                                [])
                            {
                                ParentTypes = new ParentTypes(true, true, true),
                            };
                        });
                        beatmapObjects.SetParent(specificObjectParent.Id.String, parentId);
                        beatmapObjects.SetParent(mappedChildId, specificObjectParent.Id.String);
                    }
                    // If object is not in beatmap, parent to global prefab parent
                    else
                    {
                        beatmapObjects.SetParent(mappedChildId, globalPrefabParent.Id.String);
                    }
                }
                else
                {
                    // No parent, parent to global prefab parent
                    beatmapObjects.SetParent(mappedChildId, globalPrefabParent.Id.String);
                }
            }
        }

        return timeline;
    }

    private IndexedItemFactory<BeatmapObject> CreateBeatmapObjectFactory(IObject @object, string? id, params object[] seeds)
    {
        var objectId = id ?? ((IIdentifiable<string>) @object).Id;
        var positionAnimation = EnumerateSequenceKeyframes(@object.PositionEvents, seeds: [..seeds, 0]);
        var scaleAnimation = EnumerateSequenceKeyframes(@object.ScaleEvents, seeds: [..seeds, 1]);
        var rotationAnimation = EnumerateRotationSequenceKeyframes(@object.RotationEvents, true, seeds: [..seeds, 2]);
        var themeColorAnimation = EnumerateThemeColorKeyframes(@object.ColorEvents);

        var parentPositionTimeOffset = @object.ParentOffset.Position;
        var parentScaleTimeOffset = @object.ParentOffset.Scale;
        var parentRotationTimeOffset = @object.ParentOffset.Rotation;

        var parentAnimatePosition = @object.ParentType.HasFlag(ParentType.Position);
        var parentAnimateScale = @object.ParentType.HasFlag(ParentType.Scale);
        var parentAnimateRotation = @object.ParentType.HasFlag(ParentType.Rotation);

        var renderMode = @object.RenderType switch
        {
            RenderType.Normal => RenderMode.Normal,
            RenderType.LeftToRightGradient => RenderMode.LeftToRightGradient,
            RenderType.RightToLeftGradient => RenderMode.RightToLeftGradient,
            RenderType.InwardsGradient => RenderMode.InwardsGradient,
            RenderType.OutwardsGradient => RenderMode.OutwardsGradient,
            _ => throw new ArgumentOutOfRangeException(nameof(IObject.RenderType))
        };
        
        var origin = new Vector2(@object.Origin.X, @object.Origin.Y);

        // TODO: Flag empty objects
        return numericId => new BeatmapObject(
            new ObjectId(objectId, numericId),
            positionAnimation,
            scaleAnimation,
            rotationAnimation,
            themeColorAnimation)
        {
            Name = @object.Name,
            IsEmpty = @object.Type is ObjectType.Empty or ObjectType.LegacyEmpty,
            ParentTemporalOffsets = new ParentTemporalOffsets(
                parentPositionTimeOffset,
                parentScaleTimeOffset,
                parentRotationTimeOffset),
            ParentTypes = new ParentTypes(
                parentAnimatePosition,
                parentAnimateScale,
                parentAnimateRotation),
            RenderMode = renderMode,
            AutoKillType = @object.AutoKillType,
            StartTime = @object.StartTime,
            KillTimeOffset = @object.AutoKillOffset,
            Origin = origin,
            RenderDepth = @object.RenderDepth,
            Shape = @object.Shape,
        };
    }
    
    private IEnumerable<BeatmapObjectColorKeyframe> EnumerateThemeColorKeyframes(IEnumerable<FixedKeyframe<ThemeColor>> events)
        => events.Select(e => new BeatmapObjectColorKeyframe
        {
            Time = e.Time,
            Ease = e.Ease,
            ColorIndex1 = e.Value.Index,
            ColorIndex2 = e.Value.EndIndex,
            Opacity = e.Value.Opacity,
        });

    private IEnumerable<PositionScaleKeyframe> EnumerateSequenceKeyframes(
        IEnumerable<Keyframe<System.Numerics.Vector2>> events,
        bool additive = false,
        params object[] seeds)
    {
        var value = Vector2.Zero;
        var i = 0;
        foreach (var @event in events)
        {
            var time = @event.Time;
            var parsedValue = ParseRandomVector2(@event, [..seeds, i++]);
            var newValue = new Vector2(parsedValue.X, parsedValue.Y);
            value = additive ? value + newValue : newValue;
            yield return new PositionScaleKeyframe
            {
                Time = time,
                Ease = @event.Ease,
                Value = value,
            };
        }
    }
    
    private IEnumerable<RotationKeyframe> EnumerateRotationSequenceKeyframes(
        IEnumerable<Keyframe<float>> events,
        bool additive = false,
        params object[] seeds)
    {
        var value = 0.0f;
        var i = 0;
        foreach (var @event in events)
        {
            var time = @event.Time;
            var newValue = MathHelper.DegreesToRadians(ParseRandomFloat(@event, [..seeds, i++]));
            value = additive ? value + newValue : newValue;
            yield return new RotationKeyframe
            {
                Time = time,
                Ease = @event.Ease,
                Value = value,
            };
        }
    }

    private float ParseRandomFloat(Keyframe<float> keyframe, params object[] seeds)
        => keyframe.RandomMode switch
        {
            RandomMode.None => keyframe.Value,
            RandomMode.Range => RoundToNearest(RandomRange(keyframe.Value, keyframe.RandomValue, seeds), keyframe.RandomInterval),
            RandomMode.Snap => MathF.Round(RandomRange(keyframe.Value, keyframe.Value + keyframe.RandomInterval, seeds)),
            RandomMode.Select => RandomRange(0.0f, 1.0f, seeds) < 0.5f ? keyframe.Value : keyframe.RandomValue,
            _ => keyframe.Value
        };

    private System.Numerics.Vector2 ParseRandomVector2(Keyframe<System.Numerics.Vector2> keyframe, params object[] seeds)
        => keyframe.RandomMode switch
        {
            RandomMode.None => keyframe.Value,
            RandomMode.Range => new System.Numerics.Vector2(
                RoundToNearest(RandomRange(keyframe.Value.X, keyframe.RandomValue.X, [..seeds, 0]), keyframe.RandomInterval),
                RoundToNearest(RandomRange(keyframe.Value.Y, keyframe.RandomValue.Y, [..seeds, 1]), keyframe.RandomInterval)),
            RandomMode.Snap => new System.Numerics.Vector2(
                MathF.Round(RandomRange(keyframe.Value.X, keyframe.Value.X + keyframe.RandomInterval, [..seeds, 0])),
                MathF.Round(RandomRange(keyframe.Value.Y, keyframe.Value.Y + keyframe.RandomInterval, [..seeds, 1]))),
            RandomMode.Select => RandomRange(0.0f, 1.0f, seeds) < 0.5f ? keyframe.Value : keyframe.RandomValue,
            RandomMode.Scale => keyframe.Value * RandomRange(keyframe.RandomValue.X, keyframe.RandomValue.Y, seeds),
            _ => keyframe.Value
        };
    
    private float RoundToNearest(float value, float nearest)
    {
        if (nearest == 0.0f)
            return value;
        
        return MathF.Round(value / nearest) * nearest;
    }
    
    private static float RandomRange(float min, float max, params object[] seeds)
    {
        // Use xxHash to generate a random number
        var hash = new XxHash32();
        
        // Hash the seeds
        foreach (var seed in seeds)
        {
            switch (seed)
            {
                case string strSeed:
                    hash.Append(Encoding.UTF8.GetBytes(strSeed));
                    break;
                case int intSeed:
                    hash.Append(BitConverter.GetBytes(intSeed));
                    break;
                case ulong ulongSeed:
                    hash.Append(BitConverter.GetBytes(ulongSeed));
                    break;
                default:
                    throw new ArgumentException($"Unsupported seed type: '{seed.GetType()}'");
            }
        }
        
        // Hash the min and max values
        hash.Append(BitConverter.GetBytes(min));
        hash.Append(BitConverter.GetBytes(max));
        
        // Get the hash as a float
        var hashValue = hash.GetCurrentHashAsUInt32();
        
        return (float) MathUtil.Lerp(min, max, hashValue / (double) uint.MaxValue);
    }
}
