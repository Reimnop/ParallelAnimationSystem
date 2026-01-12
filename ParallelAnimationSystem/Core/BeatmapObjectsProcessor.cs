using System.Numerics;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Beatmap;
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
            
            if (parentType.HasFlag(ParentType.Scale))
            {
                var scale = beatmapObject.ScaleSequence.Interpolate(time - beatmapObject.StartTime - parentOffset.Scale, context);
                matrix *= Matrix3x2.CreateScale(scale);
            }

            if (parentType.HasFlag(ParentType.Rotation))
            {
                var rotation = beatmapObject.RotationSequence.Interpolate(time - beatmapObject.StartTime - parentOffset.Rotation, context);
                matrix *= Matrix3x2.CreateRotation(rotation);
            }

            if (parentType.HasFlag(ParentType.Position))
            {
                var position = beatmapObject.PositionSequence.Interpolate(time - beatmapObject.StartTime - parentOffset.Position, context);
                matrix *= Matrix3x2.CreateTranslation(position);
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