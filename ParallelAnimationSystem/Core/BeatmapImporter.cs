using System.IO.Hashing;
using System.Text;
using Microsoft.Extensions.Logging;
using OpenTK.Mathematics;
using Pamx;
using Pamx.Common;
using Pamx.Common.Data;
using Pamx.Common.Enum;
using Pamx.Common.Implementation;
using ParallelAnimationSystem.Core.Animation;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering.TextProcessing;
using ParallelAnimationSystem.Util;
using TmpParser;

namespace ParallelAnimationSystem.Core;

public class BeatmapImporter(ulong randomSeed, ILogger logger)
{
    public AnimationRunner CreateRunner(IBeatmap beatmap, bool isLsb)
    {
        // Convert all the objects in the beatmap to GameObjects
        var gameObjects = CreateGameObjects(beatmap, isLsb);
        
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
        
        // Create the runner with the GameObjects
        return new AnimationRunner(
            gameObjects, 
            themeColorSequence, 
            cameraPositionSequence, 
            cameraScaleSequence, 
            cameraRotationSequence,
            bloomSequence,
            hueSequence,
            lensDistortionSequence);
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
                if (theme is null)
                    throw new NotImplementedException("TODO: Implement built-in themes");
                var ease = EaseFunctions.GetOrDefault(x.Ease, EaseFunctions.Linear);
                return new Animation.Keyframe<ITheme>(time, theme, ease);
            });
        return new Sequence<ITheme, ThemeColors>(keyframes, InterpolateTheme);
    }

    private ThemeColors InterpolateTheme(ITheme a, ITheme b, float t, object? context)
    {
        var themeColors = new ThemeColors();
        themeColors.Background = InterpolateColor4(a.Background.ToColor4(), b.Background.ToColor4(), t);
        themeColors.Gui = InterpolateColor4(a.Gui.ToColor4(), b.Gui.ToColor4(), t);
        themeColors.GuiAccent = InterpolateColor4(a.GuiAccent.ToColor4(), b.GuiAccent.ToColor4(), t);
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

    private IEnumerable<GameObject> CreateGameObjects(IBeatmap beatmap, bool isLsb)
    {
        return beatmap.Objects
            .Concat(beatmap.PrefabObjects
                .SelectMany(ExpandPrefabObject))
            .Indexed()
            .Select(x => CreateGameObject(x.Index, x.Value, isLsb))
            .OfType<GameObject>();
    }
        
    private IEnumerable<IObject> ExpandPrefabObject(IPrefabObject prefabObject)
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
            prefabObject.Rotation));
        
        // Clone all the objects in the prefab
        if (prefabObject.Prefab is not IPrefab prefab)
            throw new InvalidOperationException("Prefab object does not have a prefab");
        
        var extraObjects = new List<IObject> { parent };
        
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
            newObj.StartTime = obj.StartTime + prefabObject.Time - prefab.Offset;
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
            else
            {
                // This code is cursed, but it works
                // Don't ask me how, it's just how PA works
                
                // It handles the case where the parent object has transformation,
                // but also the object that this object references outside the prefab
                // also has its own transformation, essentially creating a situation
                // where a single object has two parents
                
                // It handles it by inserting an empty object in the middle of the original
                // object that the prefab object references and this object
                
                // The behavior is supposed to be undefined, but this is how PA handles it
                var idkAnymore = Asset.CreateObject();
                idkAnymore.Parent = currentParent;
                idkAnymore.Type = ObjectType.Empty;
                idkAnymore.PositionEvents.Add(new Pamx.Common.Data.Keyframe<System.Numerics.Vector2>(
                    0.0f, 
                    new System.Numerics.Vector2(prefabObject.Position.X, prefabObject.Position.Y)));
                idkAnymore.ScaleEvents.Add(new Pamx.Common.Data.Keyframe<System.Numerics.Vector2>(
                    0.0f,
                    new System.Numerics.Vector2(prefabObject.Scale.X, prefabObject.Scale.Y)));
                idkAnymore.RotationEvents.Add(new Pamx.Common.Data.Keyframe<float>(
                    0.0f,
                    prefabObject.Rotation));
                idkAnymore.ParentType = newObj.ParentType;
                idkAnymore.ParentOffset = newObj.ParentOffset;
                extraObjects.Add(idkAnymore);
                
                newObj.Parent = idkAnymore;
                newObj.ParentType = ParentType.Position | ParentType.Scale | ParentType.Rotation;
                newObj.ParentOffset = default;
            }
        }

        // Objects with no parent should be parented to the global parent
        foreach (var newObj in lookup.Values)
        {
            if (newObj.Parent is not null)
                continue;
            newObj.Parent = parent;
            newObj.ParentType = ParentType.Position | ParentType.Scale | ParentType.Rotation;
            newObj.ParentOffset = default;
        }
        
        return extraObjects.Concat(lookup.Values);
    }

    private GameObject? CreateGameObject(int i, IObject beatmapObject, bool isLsb)
    {
        // We can skip empty objects to save memory
        if (beatmapObject.Type is ObjectType.Empty or ObjectType.LegacyEmpty)
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
        
        if (killTime <= startTime) 
            return null; // This object can be safely discarded, as it's never alive

        var parentPositionTimeOffset = beatmapObject.ParentOffset.Position;
        var parentScaleTimeOffset = beatmapObject.ParentOffset.Scale;
        var parentRotationTimeOffset = beatmapObject.ParentOffset.Rotation;

        var parentAnimatePosition = beatmapObject.ParentType.HasFlag(ParentType.Position);
        var parentAnimateScale = beatmapObject.ParentType.HasFlag(ParentType.Scale);
        var parentAnimateRotation = beatmapObject.ParentType.HasFlag(ParentType.Rotation);

        var renderMode = beatmapObject.RenderType switch
        {
            RenderType.Normal => RenderMode.Normal,
            RenderType.LeftToRightGradient => RenderMode.LeftToRightGradient,
            RenderType.RightToLeftGradient => RenderMode.RightToLeftGradient,
            RenderType.InwardsGradient => RenderMode.InwardsGradient,
            RenderType.OutwardsGradient => RenderMode.OutwardsGradient,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        var origin = new Vector2(beatmapObject.Origin.X, beatmapObject.Origin.Y);

        var shapeIndex = (int) beatmapObject.Shape;
        var shapeOptionIndex = beatmapObject.ShapeOption;
        var text = beatmapObject.Shape == ObjectShape.Text ? beatmapObject.Text : null;
        var horizontalAlignment = HorizontalAlignment.Center;
        var verticalAlignment = VerticalAlignment.Center;

        if (!isLsb && beatmapObject.Shape == ObjectShape.Text)
        {
            horizontalAlignment = beatmapObject.Origin.X switch
            {
                -0.5f => HorizontalAlignment.Left,
                0.5f => HorizontalAlignment.Right,
                _ => HorizontalAlignment.Center,
            };
            
            verticalAlignment = beatmapObject.Origin.Y switch
            {
                -0.5f => VerticalAlignment.Top,
                0.5f => VerticalAlignment.Bottom,
                _ => VerticalAlignment.Center,
            };
            
            origin = Vector2.Zero;
        }
        
        var parentTransforms = new List<ParentTransform>();
        try
        {
            if (beatmapObject.Parent is IObject parentObject)
                RecursivelyCreateParentTransform(parentObject, [], parentTransforms);
        }
        catch (InvalidOperationException e)
        {
            logger.LogError(e, "Failed to create parent transform for object '{ObjectId}'", objectId);
            return null;
        }
        
        var depth = MathHelper.MapRange(beatmapObject.RenderDepth + parentTransforms.Count * 0.001f - i * 0.00001f, -100.0f, 100.0f, 0.0f, 1.0f);
        
        return new GameObject(
            startTime,
            killTime,
            positionAnimation, scaleAnimation, rotationAnimation, themeColorAnimation,
            -parentPositionTimeOffset, -parentScaleTimeOffset, -parentRotationTimeOffset,
            parentAnimatePosition, parentAnimateScale, parentAnimateRotation,
            renderMode, origin, 
            shapeIndex, shapeOptionIndex, depth,
            text, horizontalAlignment, verticalAlignment,
            parentTransforms);
    }

    private void RecursivelyCreateParentTransform(IObject beatmapObject, HashSet<string> visited, List<ParentTransform> parentTransforms)
    {
        var objectId = ((IIdentifiable<string>) beatmapObject).Id;
        if (!visited.Add(objectId))
            throw new InvalidOperationException("Circular parent reference detected");

        var positionAnimation = CreateSequence(beatmapObject.PositionEvents, seed: objectId);
        var scaleAnimation = CreateSequence(beatmapObject.ScaleEvents, seed: objectId + "1");
        var rotationAnimation = CreateRotationSequence(beatmapObject.RotationEvents, true, objectId + "2");
        
        var parentPositionTimeOffset = beatmapObject.ParentOffset.Position;
        var parentScaleTimeOffset = beatmapObject.ParentOffset.Scale;
        var parentRotationTimeOffset = beatmapObject.ParentOffset.Rotation;

        var parentAnimatePosition = beatmapObject.ParentType.HasFlag(ParentType.Position);
        var parentAnimateScale = beatmapObject.ParentType.HasFlag(ParentType.Scale);
        var parentAnimateRotation = beatmapObject.ParentType.HasFlag(ParentType.Rotation);
        
        parentTransforms.Add(new ParentTransform(
            -beatmapObject.StartTime,
            positionAnimation, scaleAnimation, rotationAnimation,
            -parentPositionTimeOffset, -parentScaleTimeOffset, -parentRotationTimeOffset,
            parentAnimatePosition, parentAnimateScale, parentAnimateRotation));

        if (beatmapObject.Parent is IObject parentObject)
            RecursivelyCreateParentTransform(parentObject, visited, parentTransforms);
    }

    private Sequence<Vector2, Vector2> CreateSequence(
        IEnumerable<Pamx.Common.Data.Keyframe<System.Numerics.Vector2>> events,
        bool additive = false,
        string seed = "")
    {
        return new Sequence<Vector2, Vector2>(EnumerateSequenceKeyframes(events, additive, seed), InterpolateVector2);
    }
    
    private Sequence<float, float> CreateRotationSequence(
        IEnumerable<Pamx.Common.Data.Keyframe<float>> events,
        bool additive = false,
        string seed = "")
    {
        return new Sequence<float, float>(EnumerateRotationSequenceKeyframes(events, additive, seed), InterpolateFloat);
    }
    
    private Sequence<ThemeColor, (Color4<Rgba>, Color4<Rgba>)> CreateThemeColorSequence(IEnumerable<FixedKeyframe<ThemeColor>> events)
    {
        var keyframes = events.Select(e =>
        {
            var time = e.Time;
            var value = e.Value;
            var ease = EaseFunctions.GetOrDefault(e.Ease, EaseFunctions.Linear);
            return new Animation.Keyframe<ThemeColor>(time, value, ease);
        });
        return new Sequence<ThemeColor, (Color4<Rgba>, Color4<Rgba>)>(keyframes, InterpolateThemeColor);
    }

    private IEnumerable<Animation.Keyframe<Vector2>> EnumerateSequenceKeyframes(
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
    
    private IEnumerable<Animation.Keyframe<float>> EnumerateRotationSequenceKeyframes(
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

    private Vector2 InterpolateVector2(Vector2 a, Vector2 b, float t, object? context)
    {
        return new Vector2(
            MathUtil.Lerp(a.X, b.X, t),
            MathUtil.Lerp(a.Y, b.Y, t));
    }
    
    private float InterpolateFloat(float a, float b, float t, object? context)
    {
        return MathUtil.Lerp(a, b, t);
    }
    
    private (Color4<Rgba>, Color4<Rgba>) InterpolateThemeColor(ThemeColor a, ThemeColor b, float t, object? context)
    {
        if (context is not ThemeColors colors)
            throw new ArgumentException($"Context is not of type {typeof(ThemeColors)}");

        var opacityA = a.Opacity;
        var opacityB = b.Opacity;
        var colorAStart = colors.Object[a.Index];
        var colorAEnd = colors.Object[a.EndIndex];
        var colorBStart = colors.Object[b.Index];
        var colorBEnd = colors.Object[b.EndIndex];

        var opacity = MathUtil.Lerp(opacityA, opacityB, t);
        var color1 = new Color4<Rgba>(
            MathUtil.Lerp(colorAStart.X, colorBStart.X, t),
            MathUtil.Lerp(colorAStart.Y, colorBStart.Y, t),
            MathUtil.Lerp(colorAStart.Z, colorBStart.Z, t),
            opacity);
        var color2 = new Color4<Rgba>(
            MathUtil.Lerp(colorAEnd.X, colorBEnd.X, t),
            MathUtil.Lerp(colorAEnd.Y, colorBEnd.Y, t),
            MathUtil.Lerp(colorAEnd.Z, colorBEnd.Z, t),
            opacity);
        return (color1, color2);
    }

    private float ParseRandomFloat(Pamx.Common.Data.Keyframe<float> keyframe, string seed, int seed2)
        => keyframe.RandomMode switch
        {
            RandomMode.None => keyframe.Value,
            RandomMode.Range => RoundToNearest(RandomRange(keyframe.Value, keyframe.RandomValue, seed, seed2), keyframe.RandomInterval),
            RandomMode.Snap => MathF.Round(RandomRange(keyframe.Value, keyframe.Value + keyframe.RandomInterval, seed, seed2)),
            RandomMode.Select => RandomRange(0.0f, 1.0f, seed, seed2) < 0.5f ? keyframe.Value : keyframe.RandomValue,
            _ => keyframe.Value
        };

    private System.Numerics.Vector2 ParseRandomVector2(Pamx.Common.Data.Keyframe<System.Numerics.Vector2> keyframe, string seed, int seed2)
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
    
    private float RoundToNearest(float value, float nearest)
    {
        if (nearest == 0.0f)
            return value;
        
        return MathF.Round(value / nearest) * nearest;
    }
    
    private float RandomRange(float min, float max, string seed, int seed2)
    {
        // Use xxHash to generate a random number
        var hash = new XxHash32();
        
        // Hash the base seed
        hash.Append(BitConverter.GetBytes(randomSeed));
        
        // Hash the seed and the seed2
        hash.Append(Encoding.UTF8.GetBytes(seed));
        hash.Append(BitConverter.GetBytes(seed2));
        
        // Hash the min and max values
        hash.Append(BitConverter.GetBytes(min));
        hash.Append(BitConverter.GetBytes(max));
        
        // Get the hash as a float
        var hashValue = hash.GetCurrentHashAsUInt32();
        
        return (float) MathUtil.Lerp(min, max, hashValue / (double) uint.MaxValue);
    }
}
