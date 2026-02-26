using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core.Service;

public class AnimationPipeline(Timeline timeline, PlaybackObjectContainer playbackObjects)
{
    private static readonly Comparer<ObjectDrawItem> comparer = Comparer<ObjectDrawItem>.Create((x, y) =>
    {
        var primaryComparison = x.SortKeyPrimary.CompareTo(y.SortKeyPrimary);
        if (primaryComparison != 0)
            return primaryComparison;
        var secondaryComparison = x.SortKeySecondary.CompareTo(y.SortKeySecondary);
        if (secondaryComparison != 0)
            return secondaryComparison;
        return x.SortKeyTertiary.CompareTo(y.SortKeyTertiary);
    });
    
    private const float TextScaleFactor = 3.0f / 32.0f;
    private static readonly Matrix3x2 TextScaleMatrix = FastMatrix.GetScaleMatrix(TextScaleFactor, TextScaleFactor);
    
    private const int InitialCacheCapacity = 1000;
    
    private ObjectDrawItem[] drawItemCache = new ObjectDrawItem[InitialCacheCapacity];
    
    private float currentTime;
    private ThemeColorState? currentThemeColorState;

    public Span<ObjectDrawItem> ComputeDrawItems(float time, ThemeColorState themeColorState)
    {
        currentTime = time;
        currentThemeColorState = themeColorState;
        
        var aliveObjects = timeline.ComputeAliveObjects(time);
        var count = aliveObjects.Count;
        
        EnsureCacheCapacity(count);

        Parallel.ForEach(aliveObjects, ProcessPlaybackObject);
        
        // sort orderedObjectIndices based on the corresponding draw items
        Array.Sort(drawItemCache, 0, count, comparer);

        return drawItemCache.AsSpan(0, count);
    }

    private void ProcessPlaybackObject(int objectIndex, ParallelLoopState loopState, long cacheIndex)
    {
        if (!playbackObjects.TryGetItem(objectIndex, out var playbackObject))
            return;
        
        var transform = CalculateObjectTransform(
            objectIndex,
            ParentType.All, ParentOffset.Zero,
            currentTime, out var parentDepth, out var renderLayer);
        
        // use origin matrix if shape is not text
        var originMatrix = playbackObject.Shape == ObjectShape.Text 
            ? Matrix3x2.Identity 
            : Matrix3x2.CreateTranslation(playbackObject.Origin);
        
        // apply text scale if the shape is text
        var textScale = playbackObject.Shape == ObjectShape.Text 
            ? TextScaleMatrix 
            : Matrix3x2.Identity;

        Debug.Assert(currentThemeColorState is not null);
        var color = playbackObject.ColorSequence.ComputeValueAt(currentTime - playbackObject.StartTime, currentThemeColorState);
        
        // calculate sort keys
        // ----- PRIMARY KEY -----
        // [8 bits layer][32 bits reversed renderDepth][16 bits reversed parentDepth]
        
        var layerKey = (ulong)renderLayer << 48;
        var renderDepthKey = (ulong)(uint.MaxValue - NumberUtil.FloatToOrderedUInt(playbackObject.RenderDepth)) << 16;
        var parentDepthKey = (ulong)(ushort.MaxValue - parentDepth);

        var primaryKey = layerKey | renderDepthKey | parentDepthKey;
        
        // ----- SECONDARY KEY -----
        // [32 bits startTime]
        var secondaryKey = (ulong)NumberUtil.FloatToOrderedUInt(playbackObject.StartTime);
        
        // ----- TERTIARY KEY -----
        // [64 bits object id]
        var tertiaryKey = playbackObject.Id.Value;
        
        // build draw item
        ref var drawItem = ref drawItemCache[cacheIndex];
        
        drawItem.SortKeyPrimary = primaryKey;
        drawItem.SortKeySecondary = secondaryKey;
        drawItem.SortKeyTertiary = tertiaryKey;
        drawItem.Transform = originMatrix * textScale * transform;
        drawItem.Color1 = color.Color1.Pack();
        drawItem.Color2 = color.Color2.Pack();
        drawItem.Opacity = color.Opacity;
        drawItem.ObjectIndex = objectIndex;
    }
    
    private Matrix3x2 CalculateObjectTransform(
        int playbackObjectIndex,
        ParentType parentType, ParentOffset parentOffset,
        float time, out ushort parentDepth, out RenderLayer renderLayer)
    {
        parentDepth = 0;
        renderLayer = RenderLayer.Foreground;
        
        var matrix = Matrix3x2.Identity;
        
        if (!playbackObjects.TryGetItem(playbackObjectIndex, out var playbackObject))
            return matrix;

        while (true)
        {
            parentDepth++;

            if (playbackObject.Type is PlaybackObjectType.PrefabIntermediate or PlaybackObjectType.Camera)
                parentType = ParentType.All;
            
            if (playbackObject.Type == PlaybackObjectType.Camera)
                renderLayer = RenderLayer.Camera;

            switch (parentType)
            {
                case ParentType.Position:
                {
                    var position = playbackObject.PositionSequence.ComputeValueAt(time - playbackObject.StartTime - parentOffset.Position);
                    matrix *= FastMatrix.GetTranslationMatrix(position.X, position.Y);
                    break;
                }
                case ParentType.Scale:
                {
                    var scale = playbackObject.ScaleSequence.ComputeValueAt(time - playbackObject.StartTime - parentOffset.Scale);
                    matrix *= FastMatrix.GetScaleMatrix(scale.X, scale.Y);
                    break;
                }
                case ParentType.Rotation:
                {
                    var rotation = playbackObject.RotationSequence.ComputeValueAt(time - playbackObject.StartTime - parentOffset.Rotation);
                    matrix *= FastMatrix.GetRotationMatrix(rotation);
                    break;
                }
                case ParentType.Position | ParentType.Rotation:
                {
                    var position = playbackObject.PositionSequence.ComputeValueAt(time - playbackObject.StartTime - parentOffset.Position);
                    var rotation = playbackObject.RotationSequence.ComputeValueAt(time - playbackObject.StartTime - parentOffset.Rotation);
                    matrix *= FastMatrix.GetTranslationRotationMatrix(position.X, position.Y, rotation);
                    break;
                }
                case ParentType.Position | ParentType.Scale:
                {
                    var position = playbackObject.PositionSequence.ComputeValueAt(time - playbackObject.StartTime - parentOffset.Position);
                    var scale = playbackObject.ScaleSequence.ComputeValueAt(time - playbackObject.StartTime - parentOffset.Scale);
                    matrix *= FastMatrix.GetTranslationScaleMatrix(position.X, position.Y, scale.X, scale.Y);
                    break;
                }
                case ParentType.Rotation | ParentType.Scale:
                {
                    var rotation = playbackObject.RotationSequence.ComputeValueAt(time - playbackObject.StartTime - parentOffset.Rotation);
                    var scale = playbackObject.ScaleSequence.ComputeValueAt(time - playbackObject.StartTime - parentOffset.Scale);
                    matrix *= FastMatrix.GetRotationScaleMatrix(scale.X, scale.Y, rotation);
                    break;
                }
                case ParentType.All:
                {
                    var position = playbackObject.PositionSequence.ComputeValueAt(time - playbackObject.StartTime - parentOffset.Position);
                    var rotation = playbackObject.RotationSequence.ComputeValueAt(time - playbackObject.StartTime - parentOffset.Rotation);
                    var scale = playbackObject.ScaleSequence.ComputeValueAt(time - playbackObject.StartTime - parentOffset.Scale);
                    matrix *= FastMatrix.GetTranslationRotationScaleMatrix(
                        position.X, position.Y,
                        scale.X, scale.Y,
                        rotation);
                    break;
                }
            }
            
            parentType = playbackObject.ParentType;
            parentOffset = playbackObject.ParentOffset;

            if (!playbackObjects.TryGetParentIndex(playbackObjectIndex, out playbackObjectIndex)) 
                break;
            
            if (!playbackObjects.TryGetItem(playbackObjectIndex, out playbackObject))
                break;
        }

        return matrix;
    }
    
    private void EnsureCacheCapacity(int capacity)
    {
        if (drawItemCache.Length < capacity)
        {
            var newCapacity = Math.Max(drawItemCache.Length * 2, capacity);
            Array.Resize(ref drawItemCache, newCapacity);
        }
    }
}