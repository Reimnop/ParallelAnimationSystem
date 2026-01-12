using System.Numerics;
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
            true, true, true,
            0.0f, 0.0f, 0.0f,
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
        bool animatePosition, bool animateScale, bool animateRotation,
        float positionTimeOffset, float scaleTimeOffset, float rotationTimeOffset,
        float time, out int parentDepth,
        object? context)
    {
        parentDepth = 0;
        
        var matrix = Matrix3x2.Identity;

        while (true)
        {
            parentDepth++;
            
            if (animateScale)
            {
                var scale = beatmapObject.ScaleSequence.Interpolate(time - beatmapObject.StartTime - scaleTimeOffset, context);
                matrix *= Matrix3x2.CreateScale(scale);
            }

            if (animateRotation)
            {
                var rotation = beatmapObject.RotationSequence.Interpolate(time - beatmapObject.StartTime - rotationTimeOffset, context);
                matrix *= Matrix3x2.CreateRotation(rotation);
            }

            if (animatePosition)
            {
                var position = beatmapObject.PositionSequence.Interpolate(time - beatmapObject.StartTime - positionTimeOffset, context);
                matrix *= Matrix3x2.CreateTranslation(position);
            }

            if (!timeline.BeatmapObjects.TryGetParent(beatmapObject.Id.Numeric, out var parent) || parent is null) 
                break;

            var parentTypes = beatmapObject.ParentTypes;
            var parentTemporalOffsets = beatmapObject.ParentTemporalOffsets;
            
            animatePosition = parentTypes.Position;
            animateScale = parentTypes.Scale;
            animateRotation = parentTypes.Rotation;
            positionTimeOffset = parentTemporalOffsets.Position;
            scaleTimeOffset = parentTemporalOffsets.Scale;
            rotationTimeOffset = parentTemporalOffsets.Rotation;
            beatmapObject = parent;
        }

        return matrix;
    }
}