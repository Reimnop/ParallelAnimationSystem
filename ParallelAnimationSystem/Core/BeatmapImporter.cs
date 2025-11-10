using System.IO.Hashing;
using System.Text;
using Microsoft.Extensions.Logging;
using OpenTK.Mathematics;
using Pamx.Common;
using Pamx.Common.Data;
using Pamx.Common.Enum;
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
        
        // TODO: Make this work
        // Convert all prefabs in the beatmap
        // var prefabs = beatmap.Prefabs
        //     .Select(CreatePrefab)
        //     .ToDictionary(x => x.Id, x => x);
        
        // Create prefab instances
        var prefabInstances = beatmap.PrefabObjects
            .Select(x =>
            {
                var prefab = (IPrefab) x.Prefab;
                var prefabInstanceId = ((IIdentifiable<string>) x).Id;
                return (x.Time - prefab.Offset, CreatePrefab(prefab, prefabInstanceId));
            })
            .Select(x => new PrefabInstanceObject(x.Item2)
            {
                StartTime = x.Item1,
            });
        
        // Create prefab instance timeline
        var prefabInstanceTimeline = new PrefabInstanceTimeline();
        foreach (var prefabInstance in prefabInstances)
            prefabInstanceTimeline.Add(prefabInstance);

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
            prefabInstanceTimeline,
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
        var color1Index = Math.Clamp(value.ColorA, 0, context.Effect.Count - 1);
        var color2Index = Math.Clamp(value.ColorB, 0, context.Effect.Count - 1);
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
            ? context.Effect[Math.Clamp(value.Color.Value, 0, context.Effect.Count - 1)]
            : new ColorRgba(0.0f, 0.0f, 0.0f, 1.0f);
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
        var colorIndex = Math.Clamp(keyframe.Value.Color, 0, context.Effect.Count - 1);
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
                Value = x.Value,
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
        var themeColorState = new ThemeColorState
        {
            Background = keyframe.Value.Background.ToColorRgba(),
            Gui = keyframe.Value.Gui.ToColorRgba(),
            GuiAccent = keyframe.Value.GuiAccent.ToColorRgba(),
        };
        
        for (var i = 0; i < keyframe.Value.Player.Count; i++)
            themeColorState.Player.Add(keyframe.Value.Player[i].ToColorRgba());
        for (var i = keyframe.Value.Player.Count; i < 4; i++)
            themeColorState.Player.Add(new ColorRgba(1.0f, 1.0f, 1.0f, 1.0f));
        
        for (var i = 0; i < keyframe.Value.Object.Count; i++)
            themeColorState.Object.Add(keyframe.Value.Object[i].ToColorRgba());
        for (var i = keyframe.Value.Object.Count; i < 9; i++)
            themeColorState.Object.Add(new ColorRgba(1.0f, 1.0f, 1.0f, 1.0f));
        
        for (var i = 0; i < keyframe.Value.Effect.Count; i++)
            themeColorState.Effect.Add(keyframe.Value.Effect[i].ToColorRgba());
        for (var i = keyframe.Value.Effect.Count; i < 9; i++)
            themeColorState.Effect.Add(new ColorRgba(1.0f, 1.0f, 1.0f, 1.0f));
        
        for (var i = 0; i < keyframe.Value.ParallaxObject.Count; i++)
            themeColorState.ParallaxObject.Add(keyframe.Value.ParallaxObject[i].ToColorRgba());
        for (var i = keyframe.Value.ParallaxObject.Count; i < 9; i++)
            themeColorState.ParallaxObject.Add(new ColorRgba(1.0f, 1.0f, 1.0f, 1.0f));
        
        return themeColorState;
    }
    
    private static ThemeColorState InterpolateThemeColorState(ThemeColorState a, ThemeColorState b, float t)
    {
        var themeColorState = new ThemeColorState
        {
            Background = ColorRgba.Lerp(a.Background, b.Background, t),
            Gui = ColorRgba.Lerp(a.Gui, b.Gui, t),
            GuiAccent = ColorRgba.Lerp(a.GuiAccent, b.GuiAccent, t)
        };
        for (var i = 0; i < Math.Min(a.Player.Count, b.Player.Count); i++)
            themeColorState.Player.Add(ColorRgba.Lerp(a.Player[i], b.Player[i], t));
        for (var i = 0; i < Math.Min(a.Object.Count, b.Object.Count); i++)
            themeColorState.Object.Add(ColorRgba.Lerp(a.Object[i], b.Object[i], t));
        for (var i = 0; i < Math.Min(a.Effect.Count, b.Effect.Count); i++)
            themeColorState.Effect.Add(ColorRgba.Lerp(a.Effect[i], b.Effect[i], t));
        for (var i = 0; i < Math.Min(a.ParallaxObject.Count, b.ParallaxObject.Count); i++)
            themeColorState.ParallaxObject.Add(ColorRgba.Lerp(a.ParallaxObject[i], b.ParallaxObject[i], t));
        return themeColorState;
    }

    private Timeline CreateTimeline(IBeatmap beatmap)
    {
        var objectDataLookup = beatmap.Objects
            .Select(x => CreateBeatmapObjectData(x))
            .ToDictionary(x => x.id, x => x);
        
        var beatmapObjectsDictionary = new Dictionary<string, BeatmapObject>();
        
        var rootObject = BeatmapObject.DefaultRoot;
        beatmapObjectsDictionary.Add(rootObject.Id, rootObject);
        
        foreach (var (_, tuple) in objectDataLookup)
            CreateBeatmapObjectRecursively(tuple, objectDataLookup, beatmapObjectsDictionary, rootObject);

        return new Timeline(rootObject);
    }

    private Prefab CreatePrefab(IPrefab prefab, params object[] seeds)
    {
        var objectDataLookup = prefab.BeatmapObjects
            .Select(x => CreateBeatmapObjectData(x, seeds))
            .ToDictionary(x => x.id, x => x);
        
        var beatmapObjectsDictionary = new Dictionary<string, BeatmapObject>();
        var convertedPrefab = new Prefab
        {
            Id = ((IIdentifiable<string>) prefab).Id,
            Name = prefab.Name,
        };
        
        foreach (var (_, tuple) in objectDataLookup)
            CreateBeatmapObjectRecursively(tuple, objectDataLookup, beatmapObjectsDictionary, convertedPrefab.RootObject);

        return convertedPrefab;
    }

    private BeatmapObject CreateBeatmapObjectRecursively(
        (string id, string? parentId, BeatmapObjectData data) tuple,
        IReadOnlyDictionary<string, (string id, string? parentId, BeatmapObjectData data)> objectDataLookup,
        Dictionary<string, BeatmapObject> beatmapObjectsDictionary,
        BeatmapObject rootObject)
    {
        var (id, parentId, data) = tuple;
        
        if (beatmapObjectsDictionary.TryGetValue(id, out var beatmapObject))
            return beatmapObject;
        
        beatmapObject = new BeatmapObject(id, data)
        {
            Parent = parentId is not null
                ? objectDataLookup.TryGetValue(parentId, out var parentObject)
                    ? CreateBeatmapObjectRecursively(parentObject, objectDataLookup, beatmapObjectsDictionary, rootObject)
                    : rootObject
                : rootObject
        };
        beatmapObjectsDictionary.Add(id, beatmapObject);
        
        return beatmapObject;
    }

    private (string id, string? parentId, BeatmapObjectData data) CreateBeatmapObjectData(IObject @object, params object[] seeds)
    {
        var objectId = ((IIdentifiable<string>) @object).Id;
        var positionAnimation = EnumerateSequenceKeyframes(@object.PositionEvents, seeds: [..seeds, objectId, 0]);
        var scaleAnimation = EnumerateSequenceKeyframes(@object.ScaleEvents, seeds: [..seeds, objectId, 1]);
        var rotationAnimation = EnumerateRotationSequenceKeyframes(@object.RotationEvents, true, seeds: [..seeds, objectId, 2]);
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
        var data = new BeatmapObjectData(
            positionAnimation,
            scaleAnimation,
            rotationAnimation,
            themeColorAnimation)
        {
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
            ShapeCategoryIndex = (int) @object.Shape,
            ShapeIndex = @object.ShapeOption,
        };
        
        return (objectId, @object.Parent is IIdentifiable<string> parentIdentifiable ? parentIdentifiable.Id : null, data);
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
    
    private float RandomRange(float min, float max, params object[] seeds)
    {
        // Use xxHash to generate a random number
        var hash = new XxHash32();
        
        // Hash the base seed
        hash.Append(BitConverter.GetBytes(randomSeed));
        
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
