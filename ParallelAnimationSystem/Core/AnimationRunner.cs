using System.Numerics;
using Pamx.Common;
using Pamx.Common.Data;
using ParallelAnimationSystem.Core.Animation;
using ParallelAnimationSystem.Core.Beatmap;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core;

public class AnimationRunner
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
    public IReadOnlyList<PerFrameBeatmapObjectData> PerFrameData => perFrameDataCache;

    private readonly Timeline timeline;
    private readonly Sequence<SequenceKeyframe<ITheme>, object?, ThemeColorState> themeColorSequence;
    private readonly Sequence<SequenceKeyframe<Vector2>, object?, Vector2> cameraPositionAnimation;
    private readonly Sequence<SequenceKeyframe<float>, object?, float> cameraScaleAnimation;
    private readonly Sequence<SequenceKeyframe<float>, object?, float> cameraRotationAnimation;
    private readonly Sequence<SequenceKeyframe<BloomData>, ThemeColorState, BloomEffectState> bloomSequence;
    private readonly Sequence<SequenceKeyframe<float>, object?, float> hueSequence;
    private readonly Sequence<SequenceKeyframe<LensDistortionData>, object?, LensDistortionData> lensDistortionSequence;
    private readonly Sequence<SequenceKeyframe<float>, object?, float> chromaticAberrationSequence;
    private readonly Sequence<SequenceKeyframe<VignetteData>, ThemeColorState, VignetteEffectState> vignetteSequence;
    private readonly Sequence<SequenceKeyframe<GradientData>, ThemeColorState, GradientEffectState> gradientSequence;
    private readonly Sequence<SequenceKeyframe<GlitchData>, object?, GlitchData> glitchSequence;
    private readonly Sequence<SequenceKeyframe<float>, object?, float> shakeSequence;
    
    private readonly PerFrameDataCache perFrameDataCache = new();
    private readonly BeatmapObjectsProcessor beatmapObjectsProcessor;
    private readonly PerFrameDepthComparer perFrameComparer = new();

    public AnimationRunner(
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
        this.timeline = timeline;
        this.themeColorSequence = themeColorSequence;
        this.cameraPositionAnimation = cameraPositionAnimation;
        this.cameraScaleAnimation = cameraScaleAnimation;
        this.cameraRotationAnimation = cameraRotationAnimation;
        this.bloomSequence = bloomSequence;
        this.hueSequence = hueSequence;
        this.lensDistortionSequence = lensDistortionSequence;
        this.chromaticAberrationSequence = chromaticAberrationSequence;
        this.vignetteSequence = vignetteSequence;
        this.gradientSequence = gradientSequence;
        this.glitchSequence = glitchSequence;
        this.shakeSequence = shakeSequence;
        
        beatmapObjectsProcessor = new BeatmapObjectsProcessor(perFrameDataCache, timeline);
    }

    /// <summary>
    /// Processes one frame of the animation. Do not call from multiple threads at the same time.
    /// </summary>
    /// <returns>The render data of the animation.</returns>
    public void ProcessFrame(float time, int workers = -1)
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
        
        // Reset per-frame data cache
        perFrameDataCache.Reset();
        
        // Update all objects in parallel
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = workers
        };
        beatmapObjectsProcessor.ProcessBeatmapObjects(parallelOptions, time, themeColorState);
        
        // Sort the per-frame data by depth
        perFrameDataCache.Sort(perFrameComparer);
    }
}