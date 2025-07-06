using System.Collections.Concurrent;
using OpenTK.Mathematics;
using Pamx.Common;
using Pamx.Common.Data;
using ParallelAnimationSystem.Core.Animation;
using ParallelAnimationSystem.Core.Beatmap;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Util;
using GradientData = Pamx.Common.Data.GradientData;
using VignetteData = Pamx.Common.Data.VignetteData;

namespace ParallelAnimationSystem.Core;

public class AnimationRunner2(
    Timeline timeline,
    Sequence<ITheme, ThemeColors> themeColorSequence,
    Sequence<Vector2, Vector2> cameraPositionAnimation,
    Sequence<float, float> cameraScaleAnimation,
    Sequence<float, float> cameraRotationAnimation,
    Sequence<BloomData, BloomData> bloomSequence,
    Sequence<float, float> hueSequence,
    Sequence<LensDistortionData, LensDistortionData> lensDistortionSequence,
    Sequence<float, float> chromaticAberrationSequence,
    Sequence<VignetteData, Data.VignetteData> vignetteSequence,
    Sequence<GradientData, Data.GradientData> gradientSequence,
    Sequence<GlitchData, GlitchData> glitchSequence,
    Sequence<float, float> shakeSequence)
{
    public Vector2 CameraPosition { get; private set; }
    public float CameraScale { get; private set; }
    public float CameraRotation { get; private set; }
    public BloomData Bloom { get; private set; }
    public float Hue { get; private set; }
    public LensDistortionData LensDistortion { get; private set; }
    public float ChromaticAberration { get; private set; }
    public Data.VignetteData Vignette { get; private set; }
    public Data.GradientData Gradient { get; private set; }
    public GlitchData Glitch { get; private set; }
    public float Shake { get; private set; }
    public Color4<Rgba> BackgroundColor { get; private set; }

    private readonly ConcurrentBag<PerFrameBeatmapObjectData> perFrameData = [];
    private readonly List<PerFrameBeatmapObjectData> sortedPerFrameData = [];

    private readonly PerFrameDataDepthComparer depthComparer = new(); 

    /// <summary>
    /// Processes one frame of the animation. Do not call from multiple threads at the same time.
    /// </summary>
    /// <returns>The render data of the animation.</returns>
    public IReadOnlyList<PerFrameBeatmapObjectData> ProcessFrame(float time, int workers = -1)
    {
        // Update theme
        var themeColors = themeColorSequence.Interpolate(time);
        if (themeColors is null)
            throw new InvalidOperationException("Theme colors are null, theme color sequence might not be populated");
        
        BackgroundColor = themeColors.Background;
        
        // Spawn and kill objects according to time
        timeline.ProcessFrame(time);
        
        // Update sequences
        CameraPosition = cameraPositionAnimation.Interpolate(time, themeColors);
        CameraScale = cameraScaleAnimation.Interpolate(time, themeColors);
        CameraRotation = cameraRotationAnimation.Interpolate(time, themeColors);
        Bloom = bloomSequence.Interpolate(time, themeColors);
        Hue = hueSequence.Interpolate(time, themeColors);
        LensDistortion = lensDistortionSequence.Interpolate(time, themeColors);
        ChromaticAberration = chromaticAberrationSequence.Interpolate(time, themeColors);
        Vignette = vignetteSequence.Interpolate(time, themeColors);
        Gradient = gradientSequence.Interpolate(time, themeColors);
        Glitch = glitchSequence.Interpolate(time, themeColors);
        Shake = shakeSequence.Interpolate(time, themeColors);
        
        // Update all objects in parallel
        perFrameData.Clear();
        
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = workers
        };
        Parallel.ForEach(timeline.AliveObjects, parallelOptions, (x, _) => ProcessGameObject(x, themeColors, time));
        
        // Sort the per-frame data by depth
        sortedPerFrameData.Clear();
        sortedPerFrameData.AddRange(perFrameData);
        sortedPerFrameData.Sort(depthComparer);
        
        // Return the sorted per-frame data
        return sortedPerFrameData;
    }

    private void ProcessGameObject(BeatmapObject beatmapObject, ThemeColors themeColors, float time)
    {
        var parentDepth = 0;
        var transform = CalculateBeatmapObjectTransform(
            beatmapObject,
            true, true, true,
            0.0f, 0.0f, 0.0f,
            time, ref parentDepth,
            null);
        var perFrameDatum = new PerFrameBeatmapObjectData
        {
            BeatmapObject = beatmapObject,
            Transform = transform,
            Colors = CalculateBeatmapObjectThemeColor(beatmapObject, time, themeColors),
            ParentDepth = parentDepth,
        };
        perFrameData.Add(perFrameDatum);
    }

    private static Matrix3 CalculateBeatmapObjectTransform(
        BeatmapObject beatmapObject,
        bool animatePosition, bool animateScale, bool animateRotation,
        float positionTimeOffset, float scaleTimeOffset, float rotationTimeOffset,
        float time, ref int parentDepth,
        object? context)
    {
        parentDepth++;
        
        var matrix = Matrix3.Identity;
        var data = beatmapObject.Data;
        var timeOffset = -data.StartTime;
            
        if (animateScale)
        {
            var scale = data.ScaleSequence.Interpolate(time + timeOffset + scaleTimeOffset, context);
            matrix *= MathUtil.CreateScale(scale);
        }
        
        if (animateRotation)
        {
            var rotation = data.RotationSequence.Interpolate(time + timeOffset + rotationTimeOffset, context);
            matrix *= MathUtil.CreateRotation(rotation);
        }
        
        if (animatePosition)
        {
            var position = data.PositionSequence.Interpolate(time + timeOffset + positionTimeOffset, context);
            matrix *= MathUtil.CreateTranslation(position);
        }
            
        if (beatmapObject.Parent is not null)
            matrix *= CalculateBeatmapObjectTransform(
                beatmapObject.Parent,
                beatmapObject.Data.ParentTypes.Position,
                beatmapObject.Data.ParentTypes.Scale,
                beatmapObject.Data.ParentTypes.Rotation,
                beatmapObject.Data.ParentTemporalOffsets.Position,
                beatmapObject.Data.ParentTemporalOffsets.Scale,
                beatmapObject.Data.ParentTemporalOffsets.Rotation,
                time, ref parentDepth,
                context);
        
        return matrix;
    }
    
    private static (Color4<Rgba>, Color4<Rgba>) CalculateBeatmapObjectThemeColor(BeatmapObject beatmapObject, float time, object? context = null)
    {
        var data = beatmapObject.Data;
        return data.ThemeColorSequence.Interpolate(time - data.StartTime, context);
    }
}