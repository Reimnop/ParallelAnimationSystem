using System.Diagnostics;
using System.Numerics;
using Pamx.Objects;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Core.Service;

public class AnimationPipeline(Timeline timeline, PlaybackObjectContainer playbackObjects, PlaybackObjectSortingService sortingService)
{
    private static readonly Comparison<ObjectDrawItem> sortRankComparison = static (x, y) => x.SortRank.CompareTo(y.SortRank);
    
    private const float TextScaleFactor = 3.0f / 32.0f;
    private static readonly Matrix3x2 TextScaleMatrix = FastMatrix.GetScaleMatrix(TextScaleFactor, TextScaleFactor);
    
    private const int InitialCacheCapacity = 1000;
    
    private ObjectDrawItem[] drawItemCache = new ObjectDrawItem[InitialCacheCapacity];
    private uint[] objectIndexToSortRank = [];
    
    private float currentTime;
    private ThemeColorState? currentThemeColorState;

    public Span<ObjectDrawItem> ComputeDrawItems(float time, ThemeColorState themeColorState)
    {
        currentTime = time;
        currentThemeColorState = themeColorState;
        
        var aliveObjects = timeline.ComputeAliveObjects(time);
        var count = aliveObjects.Count;
        
        // ensure we have enough capacity in the cache to store all draw items
        EnsureCacheCapacity(count);

        // get the mapping from object index to sort rank
        objectIndexToSortRank = sortingService.GetObjectIndexToSortRankMapping();

        Parallel.ForEach(aliveObjects, ProcessPlaybackObject);
        
        // sort orderedObjectIndices based on the corresponding draw items
        var drawItemSpan = drawItemCache.AsSpan(0, count);
        drawItemSpan.Sort(sortRankComparison);
        return drawItemSpan;
    }

    private void ProcessPlaybackObject(int objectIndex, ParallelLoopState loopState, long cacheIndex)
    {
        if (!playbackObjects.TryGetItem(objectIndex, out var playbackObject))
            return;
        
        var transform = CalculateObjectTransform(
            objectIndex,
            ParentType.All, ParentOffset.Zero,
            currentTime);
        
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
        
        // build draw item
        ref var drawItem = ref drawItemCache[cacheIndex];
        
        drawItem.SortRank = objectIndexToSortRank[objectIndex];
        drawItem.Transform = originMatrix * textScale * transform;
        drawItem.Color1 = color.Color1;
        drawItem.Color2 = color.Color2;
        drawItem.Opacity = color.Opacity;
        drawItem.ObjectIndex = objectIndex;
    }
    
    private Matrix3x2 CalculateObjectTransform(
        int playbackObjectIndex,
        ParentType parentType, ParentOffset parentOffset,
        float time)
    {
        var matrix = Matrix3x2.Identity;
        
        if (!playbackObjects.TryGetItem(playbackObjectIndex, out var playbackObject))
            return matrix;

        while (true)
        {
            if (playbackObject.Type is PlaybackObjectType.PrefabIntermediate or PlaybackObjectType.Camera)
                parentType = ParentType.All;

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