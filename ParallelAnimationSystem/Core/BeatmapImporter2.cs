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
using GradientData = Pamx.Common.Data.GradientData;
using VignetteData = Pamx.Common.Data.VignetteData;

namespace ParallelAnimationSystem.Core;

public class BeatmapImporter2(ulong randomSeed, ILogger logger)
{
    public AnimationRunner2 CreateRunner(IBeatmap beatmap)
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
        return new AnimationRunner2(
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

    private Sequence<float, float> CreateShakeSequence(IList<FixedKeyframe<float>> shakeEvents)
    {
        var keyframes = shakeEvents
            .Select(x =>
            {
                var time = x.Time;
                var value = x.Value;
                var ease = EaseFunctions.GetOrDefault(x.Ease, EaseFunctions.Linear);
                return new Animation.Keyframe<float>(time, value, ease);
            });
        return new Sequence<float, float>(keyframes, InterpolateFloat);
    }
    
    private Sequence<GlitchData, GlitchData> CreateGlitchSequence(IList<FixedKeyframe<GlitchData>> glitchEvents)
    {
        var keyframes = glitchEvents
            .Select(x =>
            {
                var time = x.Time;
                var value = x.Value;
                var ease = EaseFunctions.GetOrDefault(x.Ease, EaseFunctions.Linear);
                return new Animation.Keyframe<GlitchData>(time, value, ease);
            });
        return new Sequence<GlitchData, GlitchData>(keyframes, InterpolateGlitchData);
    }

    private static GlitchData InterpolateGlitchData(GlitchData a, GlitchData b, float t, object? context)
    {
        return new GlitchData
        {
            Intensity = MathUtil.Lerp(a.Intensity, b.Intensity, t),
            Speed = MathUtil.Lerp(a.Speed, b.Speed, t),
            Width = MathUtil.Lerp(a.Width, b.Width, t)
        };
    }

    private Sequence<GradientData, Data.GradientData> CreateGradientSequence(IList<FixedKeyframe<GradientData>> gradientEvents)
    {
        var keyframes = gradientEvents
            .Select(x =>
            {
                var time = x.Time;
                var value = x.Value;
                var ease = EaseFunctions.GetOrDefault(x.Ease, EaseFunctions.Linear);
                return new Animation.Keyframe<GradientData>(time, value, ease);
            });
        return new Sequence<GradientData, Data.GradientData>(keyframes, InterpolateGradientData);
    }

    private Sequence<VignetteData, Data.VignetteData> CreateVignetteSequence(IList<FixedKeyframe<VignetteData>> vignetteEvents)
    {
        var keyframes = vignetteEvents
            .Select(x =>
            {
                var time = x.Time;
                var value = x.Value;
                var ease = EaseFunctions.GetOrDefault(x.Ease, EaseFunctions.Linear);
                return new Animation.Keyframe<VignetteData>(time, value, ease);
            });
        return new Sequence<VignetteData, Data.VignetteData>(keyframes, InterpolateVignetteData);
    }

    private Sequence<float, float> CreateChromaticAberrationSequence(IList<FixedKeyframe<float>> chromaEvents)
    {
        var keyframes = chromaEvents
            .Select(x =>
            {
                var time = x.Time;
                var value = x.Value;
                var ease = EaseFunctions.GetOrDefault(x.Ease, EaseFunctions.Linear);
                return new Animation.Keyframe<float>(time, value, ease);
            });
        return new Sequence<float, float>(keyframes, InterpolateFloat);
    }
    
    private Data.GradientData InterpolateGradientData(GradientData a, GradientData b, float t, object? context)
    {
        var themeColors = (ThemeColors) context!;
        
        var color1 = new Vector3(
            MathUtil.Lerp(a.ColorA >= themeColors.Effect.Count ? 1.0f : themeColors.Effect[a.ColorA].X, b.ColorA >= themeColors.Effect.Count ? 1.0f : themeColors.Effect[b.ColorA].X, t),
            MathUtil.Lerp(a.ColorA >= themeColors.Effect.Count ? 1.0f : themeColors.Effect[a.ColorA].Y, b.ColorA >= themeColors.Effect.Count ? 1.0f : themeColors.Effect[b.ColorA].Y, t),
            MathUtil.Lerp(a.ColorA >= themeColors.Effect.Count ? 1.0f : themeColors.Effect[a.ColorA].Z, b.ColorA >= themeColors.Effect.Count ? 1.0f : themeColors.Effect[b.ColorA].Z, t));
        var color2 = new Vector3(
            MathUtil.Lerp(a.ColorB >= themeColors.Effect.Count ? 1.0f : themeColors.Effect[a.ColorB].X, b.ColorB >= themeColors.Effect.Count ? 1.0f : themeColors.Effect[b.ColorB].X, t),
            MathUtil.Lerp(a.ColorB >= themeColors.Effect.Count ? 1.0f : themeColors.Effect[a.ColorB].Y, b.ColorB >= themeColors.Effect.Count ? 1.0f : themeColors.Effect[b.ColorB].Y, t),
            MathUtil.Lerp(a.ColorB >= themeColors.Effect.Count ? 1.0f : themeColors.Effect[a.ColorB].Z, b.ColorB >= themeColors.Effect.Count ? 1.0f : themeColors.Effect[b.ColorB].Z, t));

        var mode = a.Mode;
        
        return new Data.GradientData
        {
            Color1 = color1,
            Color2 = color2,
            Intensity = MathUtil.Lerp(a.Intensity, b.Intensity, t),
            Rotation = MathUtil.Lerp(a.Rotation, b.Rotation, t),
            Mode = mode
        };
    }
    
    private static Data.VignetteData InterpolateVignetteData(VignetteData a, VignetteData b, float t, object? context)
    {
        var themeColors = (ThemeColors) context!;

        var rounded = t > 0.5f ? b.Rounded : a.Rounded;
        var roundness = a.Roundness.HasValue && b.Roundness.HasValue
            ? MathUtil.Lerp(a.Roundness.Value, b.Roundness.Value, t)
            : 1.0f;
        
        var color = a.Color.HasValue && b.Color.HasValue
            ? new Vector3(
                MathUtil.Lerp(a.Color.Value >= themeColors.Effect.Count ? 0.0f : themeColors.Effect[a.Color.Value].X, b.Color.Value >= themeColors.Effect.Count ? 0.0f : themeColors.Effect[b.Color.Value].X, t),
                MathUtil.Lerp(a.Color.Value >= themeColors.Effect.Count ? 0.0f : themeColors.Effect[a.Color.Value].Y, b.Color.Value >= themeColors.Effect.Count ? 0.0f : themeColors.Effect[b.Color.Value].Y, t),
                MathUtil.Lerp(a.Color.Value >= themeColors.Effect.Count ? 0.0f : themeColors.Effect[a.Color.Value].Z, b.Color.Value >= themeColors.Effect.Count ? 0.0f : themeColors.Effect[b.Color.Value].Z, t))
            : Vector3.Zero;
        
        return new Data.VignetteData
        {
            Intensity = MathUtil.Lerp(a.Intensity, b.Intensity, t),
            Smoothness = MathUtil.Lerp(a.Smoothness, b.Smoothness, t),
            Color = color,
            Rounded = rounded,
            Roundness = roundness,
            Center = new Vector2(
                MathUtil.Lerp(a.Center.X, b.Center.X, t),
                MathUtil.Lerp(a.Center.Y, b.Center.Y, t)),
        };
    }

    private Sequence<LensDistortionData, LensDistortionData> CreateLensDistortionSequence(IList<FixedKeyframe<LensDistortionData>> lensDistortionEvents)
    {
        var keyframes = lensDistortionEvents
            .Select(x =>
            {
                var time = x.Time;
                var value = x.Value;
                var ease = EaseFunctions.GetOrDefault(x.Ease, EaseFunctions.Linear);
                return new Animation.Keyframe<LensDistortionData>(time, value, ease);
            });
        return new Sequence<LensDistortionData, LensDistortionData>(keyframes, InterpolateLensDistortionData);
    }
    
    private LensDistortionData InterpolateLensDistortionData(LensDistortionData a, LensDistortionData b, float t, object? context)
    {
        return new LensDistortionData
        {
            Intensity = MathUtil.Lerp(a.Intensity, b.Intensity, t),
            Center = new System.Numerics.Vector2(
                MathUtil.Lerp(a.Center.X, b.Center.X, t),
                MathUtil.Lerp(a.Center.Y, b.Center.Y, t))
        };
    }
    
    private Sequence<BloomData, BloomData> CreateBloomSequence(IList<FixedKeyframe<BloomData>> bloomEvents)
    {
        var keyframes = bloomEvents
            .Select(x =>
            {
                var time = x.Time;
                var value = x.Value;
                var ease = EaseFunctions.GetOrDefault(x.Ease, EaseFunctions.Linear);
                return new Animation.Keyframe<BloomData>(time, value, ease);
            });
        return new Sequence<BloomData, BloomData>(keyframes, InterpolateBloomData);
    }

    private BloomData InterpolateBloomData(BloomData a, BloomData b, float t, object? context)
    {
        return new BloomData
        {
            Intensity = MathUtil.Lerp(a.Intensity, b.Intensity, t),
            Diffusion = MathUtil.Lerp(a.Diffusion, b.Diffusion, t)
        };
    }

    private Sequence<float, float> CreateHueSequence(IList<FixedKeyframe<float>> hueEvents)
    {
        var keyframes = hueEvents
            .Select(x =>
            {
                var time = x.Time;
                var value = MathHelper.DegreesToRadians(x.Value);
                var ease = EaseFunctions.GetOrDefault(x.Ease, EaseFunctions.Linear);
                return new Animation.Keyframe<float>(time, value, ease);
            });
        return new Sequence<float, float>(keyframes, InterpolateFloat);
    }
    
    private Sequence<Vector2, Vector2> CreateCameraPositionSequence(IList<FixedKeyframe<System.Numerics.Vector2>> cameraPositionEvents)
    {
        var keyframes = cameraPositionEvents
            .Select(x =>
            {
                var time = x.Time;
                var value = new Vector2(x.Value.X, x.Value.Y);
                var ease = EaseFunctions.GetOrDefault(x.Ease, EaseFunctions.Linear);
                return new Animation.Keyframe<Vector2>(time, value, ease);
            });
        return new Sequence<Vector2, Vector2>(keyframes, InterpolateVector2);
    }
    
    private Sequence<float, float> CreateCameraScaleSequence(IList<FixedKeyframe<float>> cameraScaleEvents)
    {
        var keyframes = cameraScaleEvents
            .Select(x =>
            {
                var time = x.Time;
                var value = x.Value;
                var ease = EaseFunctions.GetOrDefault(x.Ease, EaseFunctions.Linear);
                return new Animation.Keyframe<float>(time, value, ease);
            });
        return new Sequence<float, float>(keyframes, InterpolateFloat);
    }
    
    private Sequence<float, float> CreateCameraRotationSequence(IList<FixedKeyframe<float>> cameraScaleEvents)
        {
            var keyframes = cameraScaleEvents
                .Select(x =>
                {
                    var time = x.Time;
                    var value = MathHelper.DegreesToRadians(x.Value);
                    var ease = EaseFunctions.GetOrDefault(x.Ease, EaseFunctions.Linear);
                    return new Animation.Keyframe<float>(time, value, ease);
                });
            return new Sequence<float, float>(keyframes, InterpolateFloat);
        }

    private Sequence<ITheme, ThemeColors> CreateThemeSequence(IList<FixedKeyframe<IReference<ITheme>>> themeEvents)
    {
        var keyframes = themeEvents
            .Where(x => x.Value.Value is not null)
            .Select(x =>
            {
                var time = x.Time;
                var theme = x.Value.Value;
                var ease = EaseFunctions.GetOrDefault(x.Ease, EaseFunctions.Linear);
                return new Animation.Keyframe<ITheme>(time, theme!, ease);
            });
        return new Sequence<ITheme, ThemeColors>(keyframes, InterpolateTheme);
    }

    private ThemeColors InterpolateTheme(ITheme a, ITheme b, float t, object? context)
    {
        var themeColors = new ThemeColors
        {
            Background = InterpolateColor4(a.Background.ToColor4(), b.Background.ToColor4(), t),
            Gui = InterpolateColor4(a.Gui.ToColor4(), b.Gui.ToColor4(), t),
            GuiAccent = InterpolateColor4(a.GuiAccent.ToColor4(), b.GuiAccent.ToColor4(), t)
        };
        for (var i = 0; i < Math.Min(a.Player.Count, b.Player.Count); i++)
            themeColors.Player.Add(InterpolateColor4(a.Player[i].ToColor4(), b.Player[i].ToColor4(), t));
        for (var i = 0; i < Math.Min(a.Object.Count, b.Object.Count); i++)
            themeColors.Object.Add(InterpolateColor4(a.Object[i].ToColor4(), b.Object[i].ToColor4(), t));
        for (var i = 0; i < Math.Min(a.Effect.Count, b.Effect.Count); i++)
            themeColors.Effect.Add(InterpolateColor4(a.Effect[i].ToColor4(), b.Effect[i].ToColor4(), t));
        for (var i = 0; i < Math.Min(a.ParallaxObject.Count, b.ParallaxObject.Count); i++)
            themeColors.ParallaxObject.Add(InterpolateColor4(a.ParallaxObject[i].ToColor4(), b.ParallaxObject[i].ToColor4(), t));
        return themeColors;
    }

    private Color4<Rgba> InterpolateColor4(Color4<Rgba> a, Color4<Rgba> b, float t)
    {
        return new Color4<Rgba>(
            MathUtil.Lerp(a.X, b.X, t),
            MathUtil.Lerp(a.Y, b.Y, t),
            MathUtil.Lerp(a.Z, b.Z, t),
            MathUtil.Lerp(a.W, b.W, t));
    }

    private Timeline CreateTimeline(IBeatmap beatmap)
    {
        var objectLookup = beatmap.Objects
            .Select(CreateBeatmapObjectData)
            .ToDictionary(x => x.id, x => x);

        var timeline = new Timeline();
        foreach (var (_, tuple) in objectLookup)
            CreateBeatmapObjectRecursively(tuple, objectLookup, timeline);

        return timeline;
    }

    private BeatmapObject CreateBeatmapObjectRecursively(
        (string id, string? parentId, BeatmapObjectData data) tuple,
        Dictionary<string, (string id, string? parentId, BeatmapObjectData data)> objectLookup,
        Timeline timeline)
    {
        var (id, parentId, data) = tuple;
        
        if (timeline.AllObjects.TryGetValue(id, out var beatmapObject))
            return beatmapObject;
        
        beatmapObject = new BeatmapObject(id, data)
        {
            Parent = parentId is not null
                ? objectLookup.TryGetValue(parentId, out var parentObject)
                    ? CreateBeatmapObjectRecursively(parentObject, objectLookup, timeline)
                    : timeline.RootObject
                : timeline.RootObject
        };
        
        return beatmapObject;
    }

    private (string id, string? parentId, BeatmapObjectData data) CreateBeatmapObjectData(IObject @object)
    {
        var objectId = ((IIdentifiable<string>) @object).Id;
        var positionAnimation = EnumerateSequenceKeyframes(@object.PositionEvents, seeds: [objectId, 0]);
        var scaleAnimation = EnumerateSequenceKeyframes(@object.ScaleEvents, seeds: [objectId, 1]);
        var rotationAnimation = EnumerateRotationSequenceKeyframes(@object.RotationEvents, true, seeds: [objectId, 2]);
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
    
    private IEnumerable<Animation.Keyframe<ThemeColor>> EnumerateThemeColorKeyframes(IEnumerable<FixedKeyframe<ThemeColor>> events)
    {
        var keyframes = events.Select(e =>
        {
            var time = e.Time;
            var value = e.Value;
            var ease = EaseFunctions.GetOrDefault(e.Ease, EaseFunctions.Linear);
            return new Animation.Keyframe<ThemeColor>(time, value, ease);
        });
        return keyframes;
    }

    private IEnumerable<Animation.Keyframe<Vector2>> EnumerateSequenceKeyframes(
        IEnumerable<Pamx.Common.Data.Keyframe<System.Numerics.Vector2>> events,
        bool additive = false,
        params object[] seeds)
    {
        var value = Vector2.Zero;
        var i = 0;
        foreach (var @event in events)
        {
            var time = @event.Time;
            var parsedValue = ParseRandomVector2(@event, seeds, i++);
            var newValue = new Vector2(parsedValue.X, parsedValue.Y);
            value = additive ? value + newValue : newValue;
            var ease = EaseFunctions.GetOrDefault(@event.Ease, EaseFunctions.Linear);
            yield return new Animation.Keyframe<Vector2>(time, value, ease);
        }
    }
    
    private IEnumerable<Animation.Keyframe<float>> EnumerateRotationSequenceKeyframes(
        IEnumerable<Pamx.Common.Data.Keyframe<float>> events,
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
            var ease = EaseFunctions.GetOrDefault(@event.Ease, EaseFunctions.Linear);
            yield return new Animation.Keyframe<float>(time, value, ease);
        }
    }

    private float ParseRandomFloat(Pamx.Common.Data.Keyframe<float> keyframe, params object[] seeds)
        => keyframe.RandomMode switch
        {
            RandomMode.None => keyframe.Value,
            RandomMode.Range => RoundToNearest(RandomRange(keyframe.Value, keyframe.RandomValue, seeds), keyframe.RandomInterval),
            RandomMode.Snap => MathF.Round(RandomRange(keyframe.Value, keyframe.Value + keyframe.RandomInterval, seeds)),
            RandomMode.Select => RandomRange(0.0f, 1.0f, seeds) < 0.5f ? keyframe.Value : keyframe.RandomValue,
            _ => keyframe.Value
        };

    private System.Numerics.Vector2 ParseRandomVector2(Pamx.Common.Data.Keyframe<System.Numerics.Vector2> keyframe, params object[] seeds)
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
    
    private static Vector2 InterpolateVector2(Vector2 a, Vector2 b, float t, object? context)
    {
        return new Vector2(
            MathUtil.Lerp(a.X, b.X, t),
            MathUtil.Lerp(a.Y, b.Y, t));
    }
    
    private static float InterpolateFloat(float a, float b, float t, object? context)
    {
        return MathUtil.Lerp(a, b, t);
    }
}
