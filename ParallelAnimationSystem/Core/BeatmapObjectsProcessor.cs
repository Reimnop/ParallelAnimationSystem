using System.Numerics;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Beatmap;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core;

public class BeatmapObjectsProcessor(PerFrameDataCache cache, Timeline timeline)
{
    public void ProcessBeatmapObjects(ParallelOptions parallelOptions, float time, ThemeColorState themeColorState, int cacheIndexOffset = 0)
    {
        var aliveObjects = timeline.AliveObjects;
        
        cache.EnsureSize(cacheIndexOffset + aliveObjects.Count);
        Parallel.ForEach(aliveObjects, parallelOptions, (item, _, index) => 
            ProcessBeatmapObject(item, time, themeColorState, cacheIndexOffset + (int) index));
    }
    
    private void ProcessBeatmapObject(BeatmapObject beatmapObject, float time, ThemeColorState themeColorState, int cacheIndex)
    {
        var transform = CalculateBeatmapObjectTransform(
            beatmapObject,
            ParentType.All, ParentOffset.Zero,
            time, out var parentDepth,
            null);
        var originMatrix = Matrix3x2.CreateTranslation(beatmapObject.Origin);
        var perFrameData = cache[cacheIndex];
        perFrameData.BeatmapObject = beatmapObject;
        perFrameData.Transform = originMatrix * transform;
        perFrameData.Color = beatmapObject.ThemeColorSequence.Interpolate(time - beatmapObject.StartTime, themeColorState);
        perFrameData.ParentDepth = parentDepth;
    }

    private Matrix3x2 CalculateBeatmapObjectTransform(
        BeatmapObject beatmapObject,
        ParentType parentType, ParentOffset parentOffset,
        float time, out int parentDepth,
        object? context)
    {
        parentDepth = 0;
        
        var matrix = Matrix3x2.Identity;

        while (true)
        {
            parentDepth++;

            switch (parentType)
            {
                case ParentType.Position:
                {
                    var position = beatmapObject.PositionSequence.Interpolate(time - beatmapObject.StartTime - parentOffset.Position, context);
                    matrix *= FastMatrix.GetTranslationMatrix(position.X, position.Y);
                    break;
                }
                case ParentType.Scale:
                {
                    var scale = beatmapObject.ScaleSequence.Interpolate(time - beatmapObject.StartTime - parentOffset.Scale, context);
                    matrix *= FastMatrix.GetScaleMatrix(scale.X, scale.Y);
                    break;
                }
                case ParentType.Rotation:
                {
                    var rotation = beatmapObject.RotationSequence.Interpolate(time - beatmapObject.StartTime - parentOffset.Rotation, context);
                    matrix *= FastMatrix.GetRotationMatrix(rotation);
                    break;
                }
                case ParentType.Position | ParentType.Rotation:
                {
                    var position = beatmapObject.PositionSequence.Interpolate(time - beatmapObject.StartTime - parentOffset.Position, context);
                    var rotation = beatmapObject.RotationSequence.Interpolate(time - beatmapObject.StartTime - parentOffset.Rotation, context);
                    matrix *= FastMatrix.GetTranslationRotationMatrix(position.X, position.Y, rotation);
                    break;
                }
                case ParentType.Position | ParentType.Scale:
                {
                    var position = beatmapObject.PositionSequence.Interpolate(time - beatmapObject.StartTime - parentOffset.Position, context);
                    var scale = beatmapObject.ScaleSequence.Interpolate(time - beatmapObject.StartTime - parentOffset.Scale, context);
                    matrix *= FastMatrix.GetTranslationScaleMatrix(position.X, position.Y, scale.X, scale.Y);
                    break;
                }
                case ParentType.Rotation | ParentType.Scale:
                {
                    var rotation = beatmapObject.RotationSequence.Interpolate(time - beatmapObject.StartTime - parentOffset.Rotation, context);
                    var scale = beatmapObject.ScaleSequence.Interpolate(time - beatmapObject.StartTime - parentOffset.Scale, context);
                    matrix *= FastMatrix.GetRotationScaleMatrix(scale.X, scale.Y, rotation);
                    break;
                }
                case ParentType.All:
                {
                    var position = beatmapObject.PositionSequence.Interpolate(time - beatmapObject.StartTime - parentOffset.Position, context);
                    var rotation = beatmapObject.RotationSequence.Interpolate(time - beatmapObject.StartTime - parentOffset.Rotation, context);
                    var scale = beatmapObject.ScaleSequence.Interpolate(time - beatmapObject.StartTime - parentOffset.Scale, context);
                    matrix *= FastMatrix.GetTranslationRotationScaleMatrix(
                        position.X, position.Y,
                        scale.X, scale.Y,
                        rotation);
                    break;
                }
            }

            if (!timeline.BeatmapObjects.TryGetParent(beatmapObject.Id.Numeric, out var parent) || parent is null) 
                break;

            parentType = beatmapObject.ParentType;
            parentOffset = beatmapObject.ParentOffset;
            beatmapObject = parent;
        }

        return matrix;
    }
}