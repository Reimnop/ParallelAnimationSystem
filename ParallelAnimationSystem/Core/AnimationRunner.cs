using System.Collections.Concurrent;
using OpenTK.Mathematics;
using Pamx.Common;
using Pamx.Common.Data;
using ParallelAnimationSystem.Core.Animation;
using ParallelAnimationSystem.Core.Beatmap;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core;

public class AnimationRunner(
    Timeline timeline,
    Sequence<SequenceKeyframe<ITheme>, object?, ThemeColorState> themeColorSequence,
    Sequence<SequenceKeyframe<Vector2>, object?, Vector2> cameraPositionAnimation,
    Sequence<SequenceKeyframe<float>, object?, float> cameraScaleAnimation,
    Sequence<SequenceKeyframe<float>, object?, float> cameraRotationAnimation,
    Sequence<SequenceKeyframe<BloomData>, ThemeColorState, BloomEffectState> bloomSequence,
    Sequence<SequenceKeyframe<float>, object?, float> hueSequence,
    Sequence<SequenceKeyframe<LensDistortionData>, object?, LensDistortionData> lensDistortionSequence,
    Sequence<SequenceKeyframe<float>, object?, float> chromaticAberrationSequence,
    Sequence<SequenceKeyframe<VignetteData>, ThemeColorState, VignetteEffectState> vignetteSequence,
    Sequence<SequenceKeyframe<GradientData>, ThemeColorState, GradientEffectState> gradientSequence,
    Sequence<SequenceKeyframe<GlitchData>, object?, GlitchData> glitchSequence,
    Sequence<SequenceKeyframe<float>, object?, float> shakeSequence)
{
    public Vector2 CameraPosition { get; private set; }
    public float CameraScale { get; private set; }
    public float CameraRotation { get; private set; }
    public BloomEffectState Bloom { get; private set; }
    public float Hue { get; private set; }
    public LensDistortionData LensDistortion { get; private set; }
    public float ChromaticAberration { get; private set; }
    public VignetteEffectState Vignette { get; private set; }
    public GradientEffectState Gradient { get; private set; }
    public GlitchData Glitch { get; private set; }
    public float Shake { get; private set; }
    public ColorRgb BackgroundColor { get; private set; }

    private readonly List<PerFrameBeatmapObjectData> perFrameData = [];

    /// <summary>
    /// Processes one frame of the animation. Do not call from multiple threads at the same time.
    /// </summary>
    /// <returns>The render data of the animation.</returns>
    public IReadOnlyList<PerFrameBeatmapObjectData> ProcessFrame(float time, int workers = -1)
    {
        // Update theme
        var themeColorState = themeColorSequence.Interpolate(time, null);
        
        // Set background color
        BackgroundColor = themeColorState.Background;
        
        // Update sequences
        CameraPosition = cameraPositionAnimation.Interpolate(time, null);
        CameraScale = cameraScaleAnimation.Interpolate(time, null);
        CameraRotation = cameraRotationAnimation.Interpolate(time, null);
        Bloom = bloomSequence.Interpolate(time, themeColorState);
        Hue = hueSequence.Interpolate(time, null);
        LensDistortion = lensDistortionSequence.Interpolate(time, null);
        ChromaticAberration = chromaticAberrationSequence.Interpolate(time, null);
        Vignette = vignetteSequence.Interpolate(time, themeColorState);
        Gradient = gradientSequence.Interpolate(time, themeColorState);
        Glitch = glitchSequence.Interpolate(time, null);
        Shake = shakeSequence.Interpolate(time, null);
        
        // Process next frame in timelines
        timeline.ProcessFrame(time);
        
        // Update all objects in parallel
        // Ensure per-frame data list is large enough
        var objectCount = timeline.AliveObjects.Count;
        while (perFrameData.Count < objectCount)
            perFrameData.Add(new PerFrameBeatmapObjectData());
        
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = workers
        };
        Parallel.ForEach(
            timeline.AliveObjects.Indexed(),
            parallelOptions,
            (x, _) => ProcessGameObject(x.Value, x.Index, themeColorState, time));
        
        // Sort the per-frame data by depth
        perFrameData.Sort(new PerFrameDepthComparer());
        
        // Return the sorted per-frame data
        return perFrameData;
    }

    private void ProcessGameObject(BeatmapObject beatmapObject, int index, ThemeColorState themeColorState, float time)
    {
        var transform = CalculateBeatmapObjectTransform(
            beatmapObject,
            true, true, true,
            0.0f, 0.0f, 0.0f,
            time, out var parentDepth,
            null);
        var originMatrix = MathUtil.CreateTranslation(beatmapObject.Origin);
        var perFrameDatum = perFrameData[index];
        perFrameDatum.BeatmapObject = beatmapObject;
        perFrameDatum.Transform = originMatrix * transform;
        perFrameDatum.Color = beatmapObject.ThemeColorSequence.Interpolate(time - beatmapObject.StartTime, themeColorState);
        perFrameDatum.ParentDepth = parentDepth;
    }

    private Matrix3 CalculateBeatmapObjectTransform(
        BeatmapObject beatmapObject,
        bool animatePosition, bool animateScale, bool animateRotation,
        float positionTimeOffset, float scaleTimeOffset, float rotationTimeOffset,
        float time, out int parentDepth,
        object? context)
    {
        parentDepth = 0;
        
        var matrix = Matrix3.Identity;

        while (true)
        {
            parentDepth++;
            
            if (animateScale)
            {
                var scale = beatmapObject.ScaleSequence.Interpolate(time - beatmapObject.StartTime - scaleTimeOffset, context);
                matrix *= MathUtil.CreateScale(scale);
            }

            if (animateRotation)
            {
                var rotation = beatmapObject.RotationSequence.Interpolate(time - beatmapObject.StartTime - rotationTimeOffset, context);
                matrix *= MathUtil.CreateRotation(rotation);
            }

            if (animatePosition)
            {
                var position = beatmapObject.PositionSequence.Interpolate(time - beatmapObject.StartTime - positionTimeOffset, context);
                matrix *= MathUtil.CreateTranslation(position);
            }

            if (!timeline.BeatmapObjects.TryGetParent(beatmapObject.Id.Int, out var parent) || parent is null) 
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