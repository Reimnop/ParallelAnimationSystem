using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Core.Animation;

public class AnimationPipeline(Timeline timeline, PlaybackObjectContainer playbackObjects)
{
    private readonly struct ObjectDrawItemComparer : IComparer<ObjectDrawItem>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(ObjectDrawItem x, ObjectDrawItem y)
        {
            var renderDepthComparison = x.RenderDepth.CompareTo(y.RenderDepth);
            if (renderDepthComparison != 0) return renderDepthComparison;
            return x.ParentDepth.CompareTo(y.ParentDepth);
        }
    }
    
    private const float TextScaleFactor = 3.0f / 32.0f;
    private static readonly Matrix3x2 TextScaleMatrix = FastMatrix.GetScaleMatrix(TextScaleFactor, TextScaleFactor);
    
    private const int InitialCacheCapacity = 1000;
    
    private ObjectDrawItem[] drawItemCache = new ObjectDrawItem[InitialCacheCapacity];
    
    private float currentTime;
    private ThemeColorState? currentThemeColorState;
    
    public ReadOnlySpan<ObjectDrawItem> ComputeDrawItems(float time, ThemeColorState themeColorState)
    {
        
        currentTime = time;
        currentThemeColorState = themeColorState;
        
        var aliveObjects = timeline.ComputeAliveObjects(time);
        EnsureCacheCapacity(aliveObjects.Count);

        Parallel.ForEach(aliveObjects, ProcessPlaybackObject);

        var drawItems = drawItemCache.AsSpan(0, aliveObjects.Count);
        drawItems.Sort(new ObjectDrawItemComparer());
        
        return drawItems;
    }

    private void ProcessPlaybackObject(int objectIndex, ParallelLoopState loopState, long cacheIndex)
    {
        if (!playbackObjects.TryGetItem(objectIndex, out var playbackObject))
            return;
        
        var transform = CalculateObjectTransform(
            objectIndex,
            ParentType.All, ParentOffset.Zero,
            currentTime, out var parentDepth);
        
        // use origin matrix if shape is not text
        var originMatrix = playbackObject.Shape == ObjectShape.Text 
            ? Matrix3x2.Identity 
            : Matrix3x2.CreateTranslation(playbackObject.Origin);
        
        // apply text scale if the shape is text
        var textScale = playbackObject.Shape == ObjectShape.Text 
            ? TextScaleMatrix 
            : Matrix3x2.Identity;

        Debug.Assert(currentThemeColorState is not null);
        var color = playbackObject.ColorSequence.ComputeValueAt(currentTime, currentThemeColorState);
        
        drawItemCache[cacheIndex].Transform = originMatrix * textScale * transform;
        drawItemCache[cacheIndex].Color1 = color.Color1;
        drawItemCache[cacheIndex].Color2 = color.Color2;
        drawItemCache[cacheIndex].RenderDepth = playbackObject.RenderDepth;
        drawItemCache[cacheIndex].ParentDepth = parentDepth;
        drawItemCache[cacheIndex].Opacity = color.Opacity;
        drawItemCache[cacheIndex].ObjectIndex = objectIndex;
    }
    
    private Matrix3x2 CalculateObjectTransform(
        int playbackObjectIndex,
        ParentType parentType, ParentOffset parentOffset,
        float time, out int parentDepth)
    {
        parentDepth = 0;
        
        var matrix = Matrix3x2.Identity;
        
        if (!playbackObjects.TryGetItem(playbackObjectIndex, out var playbackObject))
            return matrix;

        while (true)
        {
            parentDepth++;

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

            if (!playbackObjects.TryGetParentIndex(playbackObjectIndex, out var parent)) 
                break;
            
            if (!playbackObjects.TryGetItem(parent, out var parentPlaybackObject))
                break;

            parentType = parentPlaybackObject.ParentType;
            parentOffset = parentPlaybackObject.ParentOffset;
            playbackObject = parentPlaybackObject;
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