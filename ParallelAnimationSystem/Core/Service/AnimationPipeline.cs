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
    private struct ObjectDrawItemIndexComparer(ObjectDrawItem[] drawItems, PlaybackObjectContainer playbackObjects) : IComparer<int>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(int i, int j)
        {
            ref var x = ref drawItems[i];
            ref var y = ref drawItems[j];
            
            var sortKeyComparison = x.SortKey.CompareTo(y.SortKey);
            if (sortKeyComparison != 0)
                return sortKeyComparison;
            
            if (!playbackObjects.TryGetItem(x.ObjectIndex, out var xObj) ||
                !playbackObjects.TryGetItem(y.ObjectIndex, out var yObj))
                return 0;
            
            var startTimeComparison = xObj.StartTime.CompareTo(yObj.StartTime);
            if (startTimeComparison != 0)
                return startTimeComparison;
            
            return xObj.Id.Value.CompareTo(yObj.Id.Value);
        }
    }
    
    private const float TextScaleFactor = 3.0f / 32.0f;
    private static readonly Matrix3x2 TextScaleMatrix = FastMatrix.GetScaleMatrix(TextScaleFactor, TextScaleFactor);
    
    private const int InitialCacheCapacity = 1000;
    
    private ObjectDrawItem[] drawItemCache = new ObjectDrawItem[InitialCacheCapacity];
    private int[] orderedObjectIndices = new int[InitialCacheCapacity];
    
    private float currentTime;
    private ThemeColorState? currentThemeColorState;
    
    public IEnumerable<ObjectDrawItem> ComputeDrawItems(float time, ThemeColorState themeColorState, out int count)
    {
        currentTime = time;
        currentThemeColorState = themeColorState;
        
        var aliveObjects = timeline.ComputeAliveObjects(time);
        count = aliveObjects.Count;
        
        EnsureCacheCapacity(count);

        Parallel.ForEach(aliveObjects, ProcessPlaybackObject);
        
        // fill orderedObjectIndices with 0..count-1
        for (var i = 0; i < count; i++)
            orderedObjectIndices[i] = i;
        
        // sort orderedObjectIndices based on the corresponding draw items
        Array.Sort(orderedObjectIndices, 0, count, new ObjectDrawItemIndexComparer(drawItemCache, playbackObjects));
        
        return EnumerateDrawItemsInRenderOrder(count);
    }
    
    private IEnumerable<ObjectDrawItem> EnumerateDrawItemsInRenderOrder(int count)
    {
        for (var i = 0; i < count; i++)
            yield return drawItemCache[orderedObjectIndices[i]];
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
        
        drawItemCache[cacheIndex].SortKey = GetSortKey(parentDepth, playbackObject.RenderDepth, renderLayer);
        drawItemCache[cacheIndex].Transform = originMatrix * textScale * transform;
        drawItemCache[cacheIndex].Color1 = color.Color1;
        drawItemCache[cacheIndex].Color2 = color.Color2;
        drawItemCache[cacheIndex].Opacity = color.Opacity;
        drawItemCache[cacheIndex].ObjectIndex = objectIndex;
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
        
        if (orderedObjectIndices.Length < capacity)
        {
            var newCapacity = Math.Max(orderedObjectIndices.Length * 2, capacity);
            Array.Resize(ref orderedObjectIndices, newCapacity);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong GetSortKey(ushort parentDepth, float renderDepth, RenderLayer layer)
    {
        // first 16 bits for parent depth (reversed)
        // next 32 bits for render depth (reversed)
        // last 16 bits for layer
        var parentDepthKey = (ulong)(ushort.MaxValue - parentDepth);
        var renderDepthKey = (ulong)(uint.MaxValue - NumberUtil.FloatToOrderedUInt(renderDepth)) << 16;
        var layerKey = (ulong)layer << 48;
        return parentDepthKey | renderDepthKey | layerKey;
    }
}