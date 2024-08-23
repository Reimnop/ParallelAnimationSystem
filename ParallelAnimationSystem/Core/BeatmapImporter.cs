using System.Diagnostics;
using System.IO.Hashing;
using System.Text;
using OpenTK.Mathematics;
using Pamx;
using Pamx.Common;
using Pamx.Common.Data;
using Pamx.Common.Enum;
using Pamx.Common.Implementation;
using ParallelAnimationSystem.Core.Animation;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core;

public static class BeatmapImporter
{
    public static AnimationRunner CreateRunner(IBeatmap beatmap)
    {
        // Convert all the objects in the beatmap to GameObjects
        var gameObjects = CreateGameObjects(beatmap);
        
        // Get theme sequence
        var themeColorSequence = CreateThemeSequence(beatmap.Events.Theme);
        
        // Get camera sequences
        var cameraPositionSequence = CreateCameraPositionSequence(beatmap.Events.Movement);
        var cameraScaleSequence = CreateCameraScaleSequence(beatmap.Events.Zoom);
        var cameraRotationSequence = CreateCameraRotationSequence(beatmap.Events.Rotation);
        
        // Get post-processing sequences
        var bloomSequence = CreateBloomSequence(beatmap.Events.Bloom);
        var hueSequence = CreateHueSequence(beatmap.Events.Hue);
        
        // Create the runner with the GameObjects
        return new AnimationRunner(
            gameObjects, 
            themeColorSequence, 
            cameraPositionSequence, 
            cameraScaleSequence, 
            cameraRotationSequence,
            bloomSequence,
            hueSequence);
    }
    
    private static Sequence<BloomData, BloomData> CreateBloomSequence(IList<FixedKeyframe<BloomData>> bloomEvents)
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

    private static BloomData InterpolateBloomData(BloomData a, BloomData b, float t, object? context)
    {
        return new BloomData
        {
            Intensity = MathHelper.Lerp(a.Intensity, b.Intensity, t),
            Diffusion = MathHelper.Lerp(a.Diffusion, b.Diffusion, t)
        };
    }

    private static Sequence<float, float> CreateHueSequence(IList<FixedKeyframe<float>> hueEvents)
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
    
    private static Sequence<Vector2, Vector2> CreateCameraPositionSequence(IList<FixedKeyframe<System.Numerics.Vector2>> cameraPositionEvents)
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
    
    private static Sequence<float, float> CreateCameraScaleSequence(IList<FixedKeyframe<float>> cameraScaleEvents)
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
    
    private static Sequence<float, float> CreateCameraRotationSequence(IList<FixedKeyframe<float>> cameraScaleEvents)
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

    private static Sequence<ITheme, ThemeColors> CreateThemeSequence(IList<FixedKeyframe<IReference<ITheme>>> themeEvents)
    {
        var keyframes = themeEvents
            .Where(x => x.Value.Value is not null)
            .Select(x =>
            {
                var time = x.Time;
                var theme = x.Value.Value;
                if (theme is null)
                    throw new NotImplementedException("TODO: Implement built-in themes");
                var ease = EaseFunctions.GetOrDefault(x.Ease, EaseFunctions.Linear);
                return new Animation.Keyframe<ITheme>(time, theme, ease);
            });
        return new Sequence<ITheme, ThemeColors>(keyframes, InterpolateTheme);
    }

    private static ThemeColors InterpolateTheme(ITheme a, ITheme b, float t, object? context)
    {
        var themeColors = new ThemeColors();
        themeColors.Background = InterpolateColor4(a.Background, b.Background, t);
        themeColors.Gui = InterpolateColor4(a.Gui, b.Gui, t);
        themeColors.GuiAccent = InterpolateColor4(a.GuiAccent, b.GuiAccent, t);
        for (var i = 0; i < Math.Min(a.Player.Count, b.Player.Count); i++)
            themeColors.Player.Add(InterpolateColor4(a.Player[i], b.Player[i], t));
        for (var i = 0; i < Math.Min(a.Object.Count, b.Object.Count); i++)
            themeColors.Object.Add(InterpolateColor4(a.Object[i], b.Object[i], t));
        for (var i = 0; i < Math.Min(a.Effect.Count, b.Effect.Count); i++)
            themeColors.Effect.Add(InterpolateColor4(a.Effect[i], b.Effect[i], t));
        for (var i = 0; i < Math.Min(a.ParallaxObject.Count, b.ParallaxObject.Count); i++)
            themeColors.ParallaxObject.Add(InterpolateColor4(a.ParallaxObject[i], b.ParallaxObject[i], t));
        return themeColors;
    }

    private static Color4 InterpolateColor4(Color4 a, Color4 b, float t)
    {
        return new Color4(
            MathHelper.Lerp(a.R, b.R, t),
            MathHelper.Lerp(a.G, b.G, t),
            MathHelper.Lerp(a.B, b.B, t),
            MathHelper.Lerp(a.A, b.A, t));
    }

    private static IEnumerable<GameObject> CreateGameObjects(IBeatmap beatmap)
    {
        return beatmap.Objects
            .Concat(beatmap.PrefabObjects
                .SelectMany(ExpandPrefabObject))
            .Indexed()
            .Select(x => CreateGameObject(x.Index, x.Value))
            .OfType<GameObject>();
    }
    
    private static IEnumerable<(int Index, T Value)> Indexed<T>(this IEnumerable<T> enumerable)
    {
        var i = 0;
        foreach (var value in enumerable)
            yield return (i++, value);
    }
        
    private static IEnumerable<IObject> ExpandPrefabObject(IPrefabObject prefabObject)
    {
        // Create a global empty object to act as the parent
        var parent = Asset.CreateObject();
        parent.Type = ObjectType.Empty;
        parent.PositionEvents.Add(new Pamx.Common.Data.Keyframe<System.Numerics.Vector2>(
            0.0f, 
            new System.Numerics.Vector2(prefabObject.Position.X, prefabObject.Position.Y)));
        parent.ScaleEvents.Add(new Pamx.Common.Data.Keyframe<System.Numerics.Vector2>(
            0.0f,
            new System.Numerics.Vector2(prefabObject.Scale.X, prefabObject.Scale.Y)));
        parent.RotationEvents.Add(new Pamx.Common.Data.Keyframe<float>(
            0.0f,
            MathHelper.DegreesToRadians(prefabObject.Rotation)));
        
        // Clone all the objects in the prefab
        if (prefabObject.Prefab is not IPrefab prefab)
            throw new InvalidOperationException("Prefab object does not have a prefab");
        
        // Create a parent lookup dictionary to correctly map the parents
        // while cloning the objects
        var lookup = new Dictionary<IObject, IObject>();
        
        foreach (var obj in prefab.BeatmapObjects)
        {
            var newObj = Asset.CreateObject();
            newObj.Name = obj.Name;
            newObj.Type = obj.Type;
            newObj.Shape = obj.Shape;
            newObj.ShapeOption = obj.ShapeOption;
            newObj.Text = obj.Text;
            newObj.Origin = obj.Origin;
            newObj.RenderDepth = obj.RenderDepth;
            newObj.StartTime = obj.StartTime + prefabObject.Time; // TODO: Prefab offset
            newObj.AutoKillType = obj.AutoKillType;
            newObj.AutoKillOffset = obj.AutoKillOffset;
            newObj.ParentType = obj.ParentType;
            newObj.ParentOffset = obj.ParentOffset;
            newObj.PositionEvents.AddRange(obj.PositionEvents);
            newObj.ScaleEvents.AddRange(obj.ScaleEvents);
            newObj.RotationEvents.AddRange(obj.RotationEvents);
            newObj.ColorEvents.AddRange(obj.ColorEvents);
            newObj.Parent = obj.Parent;
            lookup.Add(obj, newObj);
        }
        
        // Remap parents
        foreach (var newObj in lookup.Values)
        {
            if (newObj.Parent is not IObject currentParent)
                continue;
            
            if (lookup.TryGetValue(currentParent, out var newParent))
                newObj.Parent = newParent;
        }

        // Objects with no parent should be parented to the global parent
        foreach (var newObj in lookup.Values)
        {
            if (newObj.Parent is not null)
                continue;
            newObj.Parent = parent;
        }
        
        yield return parent;
        foreach (var newObj in lookup.Values)
            yield return newObj;
    }

    private static GameObject? CreateGameObject(int i, IObject beatmapObject)
    {
        // We can skip empty objects to save memory
        if (beatmapObject.Type is ObjectType.Empty or ObjectType.LegacyEmpty)
            return null;
        
        // We don't support text objects yet, so just return null
        if (beatmapObject.Shape == ObjectShape.Text)
            return null;
        
        var objectId = ((IIdentifiable<string>) beatmapObject).Id;
        var positionAnimation = CreateSequence(beatmapObject.PositionEvents, seed: objectId);
        var scaleAnimation = CreateSequence(beatmapObject.ScaleEvents, seed: objectId + "1");
        var rotationAnimation = CreateRotationSequence(beatmapObject.RotationEvents, true, objectId + "2");
        var themeColorAnimation = CreateThemeColorSequence(beatmapObject.ColorEvents);

        var maxSequenceLength = MathF.Max(
            positionAnimation.GetSequenceLength(),
            MathF.Max(
                scaleAnimation.GetSequenceLength(),
                MathF.Max(
                    rotationAnimation.GetSequenceLength(),
                    themeColorAnimation.GetSequenceLength())));
        
        var startTime = beatmapObject.StartTime;
        var killTime = beatmapObject.AutoKillType switch
        {
            AutoKillType.NoAutoKill => float.MaxValue,
            AutoKillType.LastKeyframe => startTime + maxSequenceLength,
            AutoKillType.LastKeyframeOffset => startTime + maxSequenceLength + beatmapObject.AutoKillOffset,
            AutoKillType.FixedTime => startTime + beatmapObject.AutoKillOffset,
            AutoKillType.SongTime => beatmapObject.AutoKillOffset,
            _ => throw new ArgumentOutOfRangeException()
        };

        var parentPositionTimeOffset = beatmapObject.ParentOffset.Position;
        var parentScaleTimeOffset = beatmapObject.ParentOffset.Scale;
        var parentRotationTimeOffset = beatmapObject.ParentOffset.Rotation;

        var parentAnimatePosition = beatmapObject.ParentType.HasFlag(ParentType.Position);
        var parentAnimateScale = beatmapObject.ParentType.HasFlag(ParentType.Scale);
        var parentAnimateRotation = beatmapObject.ParentType.HasFlag(ParentType.Rotation);
        
        var origin = new Vector2(beatmapObject.Origin.X, beatmapObject.Origin.Y);

        var shapeIndex = (int) beatmapObject.Shape;
        var shapeOptionIndex = beatmapObject.ShapeOption;

        var parentChainSize = 0;
        var parent = beatmapObject.Parent is IObject parentObject ? RecursivelyCreateParentTransform(parentObject, ref parentChainSize) : null;
        
        var depth = MathHelper.MapRange(beatmapObject.RenderDepth + parentChainSize * beatmapObject.RenderDepth * 0.0005f - i * 0.00001f, -100.0f, 100.0f, 0.0f, 1.0f);
        
        return new GameObject(
            startTime,
            killTime,
            positionAnimation, scaleAnimation, rotationAnimation, themeColorAnimation,
            -parentPositionTimeOffset, -parentScaleTimeOffset, -parentRotationTimeOffset,
            parentAnimatePosition, parentAnimateScale, parentAnimateRotation,
            origin, shapeIndex, shapeOptionIndex, depth, parent);
    }

    private static ParentTransform RecursivelyCreateParentTransform(IObject beatmapObject, ref int parentChainSize)
    {
        parentChainSize++;
        
        var objectId = ((IIdentifiable<string>) beatmapObject).Id;
        var positionAnimation = CreateSequence(beatmapObject.PositionEvents, seed: objectId);
        var scaleAnimation = CreateSequence(beatmapObject.ScaleEvents, seed: objectId + "1");
        var rotationAnimation = CreateRotationSequence(beatmapObject.RotationEvents, true, objectId + "2");

        var parentPositionTimeOffset = beatmapObject.ParentOffset.Position;
        var parentScaleTimeOffset = beatmapObject.ParentOffset.Scale;
        var parentRotationTimeOffset = beatmapObject.ParentOffset.Rotation;

        var parentAnimatePosition = beatmapObject.ParentType.HasFlag(ParentType.Position);
        var parentAnimateScale = beatmapObject.ParentType.HasFlag(ParentType.Scale);
        var parentAnimateRotation = beatmapObject.ParentType.HasFlag(ParentType.Rotation);
        
        var parent = beatmapObject.Parent is IObject parentObject ? RecursivelyCreateParentTransform(parentObject, ref parentChainSize) : null;
        
        return new ParentTransform(
            -beatmapObject.StartTime,
            positionAnimation, scaleAnimation, rotationAnimation,
            -parentPositionTimeOffset, -parentScaleTimeOffset, -parentRotationTimeOffset,
            parentAnimatePosition, parentAnimateScale, parentAnimateRotation,
            parent);
    }

    private static Sequence<Vector2, Vector2> CreateSequence(
        IEnumerable<Pamx.Common.Data.Keyframe<System.Numerics.Vector2>> events,
        bool additive = false,
        string seed = "")
    {
        return new Sequence<Vector2, Vector2>(EnumerateSequenceKeyframes(events, additive, seed), InterpolateVector2);
    }
    
    private static Sequence<float, float> CreateRotationSequence(
        IEnumerable<Pamx.Common.Data.Keyframe<float>> events,
        bool additive = false,
        string seed = "")
    {
        return new Sequence<float, float>(EnumerateRotationSequenceKeyframes(events, additive, seed), InterpolateFloat);
    }
    
    private static Sequence<ThemeColor, (Color4, Color4)> CreateThemeColorSequence(IEnumerable<FixedKeyframe<ThemeColor>> events)
    {
        var keyframes = events.Select(e =>
        {
            var time = e.Time;
            var value = e.Value;
            var ease = EaseFunctions.GetOrDefault(e.Ease, EaseFunctions.Linear);
            return new Animation.Keyframe<ThemeColor>(time, value, ease);
        });
        return new Sequence<ThemeColor, (Color4, Color4)>(keyframes, InterpolateThemeColor);
    }

    private static IEnumerable<Animation.Keyframe<Vector2>> EnumerateSequenceKeyframes(
        IEnumerable<Pamx.Common.Data.Keyframe<System.Numerics.Vector2>> events,
        bool additive = false,
        string seed = "")
    {
        var value = Vector2.Zero;
        var i = 0;
        foreach (var @event in events)
        {
            var time = @event.Time;
            var parsedValue = ParseRandomVector2(@event, seed, i++);
            var newValue = new Vector2(parsedValue.X, parsedValue.Y);
            value = additive ? value + newValue : newValue;
            var ease = EaseFunctions.GetOrDefault(@event.Ease, EaseFunctions.Linear);
            yield return new Animation.Keyframe<Vector2>(time, value, ease);
        }
    }
    
    private static IEnumerable<Animation.Keyframe<float>> EnumerateRotationSequenceKeyframes(
        IEnumerable<Pamx.Common.Data.Keyframe<float>> events,
        bool additive = false,
        string seed = "")
    {
        var value = 0.0f;
        var i = 0;
        foreach (var @event in events)
        {
            var time = @event.Time;
            var newValue = MathHelper.DegreesToRadians(ParseRandomFloat(@event, seed, i++));
            value = additive ? value + newValue : newValue;
            var ease = EaseFunctions.GetOrDefault(@event.Ease, EaseFunctions.Linear);
            yield return new Animation.Keyframe<float>(time, value, ease);
        }
    }

    private static Vector2 InterpolateVector2(Vector2 a, Vector2 b, float t, object? context)
    {
        return new Vector2(
            MathHelper.Lerp(a.X, b.X, t),
            MathHelper.Lerp(a.Y, b.Y, t));
    }
    
    private static float InterpolateFloat(float a, float b, float t, object? context)
    {
        return MathHelper.Lerp(a, b, t);
    }
    
    private static (Color4, Color4) InterpolateThemeColor(ThemeColor a, ThemeColor b, float t, object? context)
    {
        if (context is not ThemeColors colors)
            throw new ArgumentException($"Context is not of type {typeof(ThemeColors)}");

        var opacityA = a.Opacity;
        var opacityB = b.Opacity;
        var colorAStart = colors.Object[a.Index];
        var colorAEnd = colors.Object[a.EndIndex];
        var colorBStart = colors.Object[b.Index];
        var colorBEnd = colors.Object[b.EndIndex];

        var opacity = MathHelper.Lerp(opacityA, opacityB, t);
        var color1 = new Color4(
            MathHelper.Lerp(colorAStart.R, colorBStart.R, t),
            MathHelper.Lerp(colorAStart.G, colorBStart.G, t),
            MathHelper.Lerp(colorAStart.B, colorBStart.B, t),
            opacity);
        var color2 = new Color4(
            MathHelper.Lerp(colorAEnd.R, colorBEnd.R, t),
            MathHelper.Lerp(colorAEnd.G, colorBEnd.G, t),
            MathHelper.Lerp(colorAEnd.B, colorBEnd.B, t),
            opacity);
        return (color1, color2);
    }

    private static float ParseRandomFloat(Pamx.Common.Data.Keyframe<float> keyframe, string seed, int seed2)
        => keyframe.RandomMode switch
        {
            RandomMode.None => keyframe.Value,
            RandomMode.Range => RoundToNearest(RandomRange(keyframe.Value, keyframe.RandomValue, seed, seed2), keyframe.RandomInterval),
            RandomMode.Snap => MathF.Round(RandomRange(keyframe.Value, keyframe.Value + keyframe.RandomInterval, seed, seed2)),
            RandomMode.Select => RandomRange(0.0f, 1.0f, seed, seed2) < 0.5f ? keyframe.Value : keyframe.RandomValue,
            _ => keyframe.Value
        };

    private static System.Numerics.Vector2 ParseRandomVector2(Pamx.Common.Data.Keyframe<System.Numerics.Vector2> keyframe, string seed, int seed2)
        => keyframe.RandomMode switch
        {
            RandomMode.None => keyframe.Value,
            RandomMode.Range => new System.Numerics.Vector2(
                RoundToNearest(RandomRange(keyframe.Value.X, keyframe.RandomValue.X, seed, seed2), keyframe.RandomInterval),
                RoundToNearest(RandomRange(keyframe.Value.Y, keyframe.RandomValue.Y, seed, seed2 + 1), keyframe.RandomInterval)),
            RandomMode.Snap => new System.Numerics.Vector2(
                MathF.Round(RandomRange(keyframe.Value.X, keyframe.Value.X + keyframe.RandomInterval, seed, seed2)),
                MathF.Round(RandomRange(keyframe.Value.Y, keyframe.Value.Y + keyframe.RandomInterval, seed, seed2 + 1))),
            RandomMode.Select => RandomRange(0.0f, 1.0f, seed, seed2) < 0.5f ? keyframe.Value : keyframe.RandomValue,
            RandomMode.Scale => keyframe.Value * RandomRange(keyframe.RandomValue.X, keyframe.RandomValue.Y, seed, seed2),
            _ => keyframe.Value
        };
    
    private static float RoundToNearest(float value, float nearest)
    {
        if (nearest == 0.0f)
            return value;
        
        return MathF.Round(value / nearest) * nearest;
    }
    
    private static float RandomRange(float min, float max, string seed, int seed2)
    {
        // Use xxHash to generate a random number
        var hash = new XxHash32();
        
        // Hash the seed and the seed2
        hash.Append(Encoding.UTF8.GetBytes(seed));
        hash.Append(BitConverter.GetBytes(seed2));
        
        // Hash the min and max values
        hash.Append(BitConverter.GetBytes(min));
        hash.Append(BitConverter.GetBytes(max));
        
        // Get the hash as a float
        var hashValue = hash.GetCurrentHashAsUInt32();
        
        return (float) MathHelper.Lerp(min, max, hashValue / (double) uint.MaxValue);
    }
}