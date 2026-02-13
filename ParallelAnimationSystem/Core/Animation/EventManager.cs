using System.Numerics;
using Pamx.Common.Data;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Model;

namespace ParallelAnimationSystem.Core.Animation;

public class EventManager
{
    private readonly Sequence<Vector2> cameraPositionSequence = new(Vector2.Lerp, () => Vector2.Zero);
    private readonly Sequence<float> cameraRotationSequence = new(float.Lerp, () => 0f);
    private readonly Sequence<float> cameraScaleSequence = new(float.Lerp, () => 20f);
    private readonly Sequence<float> cameraShakeSequence = new(float.Lerp, () => 0f);
    private readonly Sequence<float> chromaSequence = new(float.Lerp, () => 0f);
    private readonly IndirectSequence<BloomData, BloomEffectState, ThemeColorState> bloomSequence = new(
        EventHelper.ResolveBloomData,
        BloomEffectState.Lerp,
        _ => default);
    private readonly IndirectSequence<VignetteData, VignetteEffectState, ThemeColorState> vignetteSequence = new(
        EventHelper.ResolveVignetteData,
        VignetteEffectState.Lerp,
        _ => default);
    private readonly Sequence<LensDistortionData> lensDistortionSequence = new(EventHelper.LerpLensDistortionData, () => default);
    private readonly Sequence<GrainData> grainSequence = new(EventHelper.LerpGrainData, () => default);
    private readonly IndirectSequence<GradientData, GradientEffectState, ThemeColorState> gradientSequence = new(
        EventHelper.ResolveGradientData,
        GradientEffectState.Lerp,
        _ => default);
    private readonly Sequence<GlitchData> glitchSequence = new(EventHelper.LerpGlitchData, () => default);
    private readonly Sequence<float> hueSequence = new(float.Lerp, () => 0f);

    private readonly EventState state = new();
    
    private BeatmapData? attachedBeatmapData;
    
    public void AttachBeatmapData(BeatmapData beatmapData)
    {
        if (attachedBeatmapData is not null)
            throw new InvalidOperationException($"A {nameof(BeatmapData)} is already attached");
        
        attachedBeatmapData = beatmapData;
        
        // load events
        cameraPositionSequence.LoadKeyframes(EventHelper.ResolveGenericKeyframes(beatmapData.Events.CameraPosition));
        cameraRotationSequence.LoadKeyframes(EventHelper.ResolveRotationKeyframes(beatmapData.Events.CameraRotation));
        cameraScaleSequence.LoadKeyframes(EventHelper.ResolveGenericKeyframes(beatmapData.Events.CameraScale));
        cameraShakeSequence.LoadKeyframes(EventHelper.ResolveGenericKeyframes(beatmapData.Events.CameraShake));
        chromaSequence.LoadKeyframes(EventHelper.ResolveGenericKeyframes(beatmapData.Events.Chroma));
        bloomSequence.LoadKeyframes(EventHelper.ResolveGenericKeyframes(beatmapData.Events.Bloom));
        vignetteSequence.LoadKeyframes(EventHelper.ResolveGenericKeyframes(beatmapData.Events.Vignette));
        lensDistortionSequence.LoadKeyframes(EventHelper.ResolveGenericKeyframes(beatmapData.Events.LensDistortion));
        grainSequence.LoadKeyframes(EventHelper.ResolveGenericKeyframes(beatmapData.Events.Grain));
        gradientSequence.LoadKeyframes(EventHelper.ResolveGenericKeyframes(beatmapData.Events.Gradient));
        glitchSequence.LoadKeyframes(EventHelper.ResolveGenericKeyframes(beatmapData.Events.Glitch));
        hueSequence.LoadKeyframes(EventHelper.ResolveRotationKeyframes(beatmapData.Events.Hue));
        
        // attach events
        beatmapData.Events.CameraPositionKeyframesChanged += OnEventsCameraPositionKeyframesChanged;
        beatmapData.Events.CameraRotationKeyframesChanged += OnEventsCameraRotationKeyframesChanged;
        beatmapData.Events.CameraScaleKeyframesChanged += OnEventsCameraScaleKeyframesChanged;
        beatmapData.Events.CameraShakeKeyframesChanged += OnEventsCameraShakeKeyframesChanged;
        beatmapData.Events.ChromaKeyframesChanged += OnEventsChromaKeyframesChanged;
        beatmapData.Events.BloomKeyframesChanged += OnBloomKeyframesChanged;
        beatmapData.Events.VignetteKeyframesChanged += OnVignetteKeyframesChanged;
        beatmapData.Events.LensDistortionKeyframesChanged += OnLensDistortionKeyframesChanged;
        beatmapData.Events.GrainKeyframesChanged += OnGrainKeyframesChanged;
        beatmapData.Events.GradientKeyframesChanged += OnGradientKeyframesChanged;
        beatmapData.Events.GlitchKeyframesChanged += OnGlitchKeyframesChanged;
        beatmapData.Events.HueKeyframesChanged += OnHueKeyframesChanged;
    }

    public void DetachBeatmapData()
    {
        if (attachedBeatmapData is null)
            return;
        
        // unload events
        cameraPositionSequence.LoadKeyframes([]);
        cameraRotationSequence.LoadKeyframes([]);
        cameraScaleSequence.LoadKeyframes([]);
        cameraShakeSequence.LoadKeyframes([]);
        chromaSequence.LoadKeyframes([]);
        bloomSequence.LoadKeyframes([]);
        vignetteSequence.LoadKeyframes([]);
        lensDistortionSequence.LoadKeyframes([]);
        grainSequence.LoadKeyframes([]);
        gradientSequence.LoadKeyframes([]);
        glitchSequence.LoadKeyframes([]);
        hueSequence.LoadKeyframes([]);
        
        // detach events
        attachedBeatmapData.Events.CameraPositionKeyframesChanged -= OnEventsCameraPositionKeyframesChanged;
        attachedBeatmapData.Events.CameraRotationKeyframesChanged -= OnEventsCameraRotationKeyframesChanged;
        attachedBeatmapData.Events.CameraScaleKeyframesChanged -= OnEventsCameraScaleKeyframesChanged;
        attachedBeatmapData.Events.CameraShakeKeyframesChanged -= OnEventsCameraShakeKeyframesChanged;
        attachedBeatmapData.Events.ChromaKeyframesChanged -= OnEventsChromaKeyframesChanged;
        attachedBeatmapData.Events.BloomKeyframesChanged -= OnBloomKeyframesChanged;
        attachedBeatmapData.Events.VignetteKeyframesChanged -= OnVignetteKeyframesChanged;
        attachedBeatmapData.Events.LensDistortionKeyframesChanged -= OnLensDistortionKeyframesChanged;
        attachedBeatmapData.Events.GrainKeyframesChanged -= OnGrainKeyframesChanged;
        attachedBeatmapData.Events.GradientKeyframesChanged -= OnGradientKeyframesChanged;
        attachedBeatmapData.Events.GlitchKeyframesChanged -= OnGlitchKeyframesChanged;
        attachedBeatmapData.Events.HueKeyframesChanged -= OnHueKeyframesChanged;
        
        attachedBeatmapData = null;
    }

    public EventState ComputeEventAt(float time, ThemeColorState tcs)
    {
        state.CameraPosition = cameraPositionSequence.ComputeValueAt(time);
        state.CameraRotation = cameraRotationSequence.ComputeValueAt(time);
        state.CameraScale = cameraScaleSequence.ComputeValueAt(time);
        state.CameraShake = cameraShakeSequence.ComputeValueAt(time);
        state.Chroma = chromaSequence.ComputeValueAt(time);
        state.Bloom = bloomSequence.ComputeValueAt(time, tcs);
        state.Vignette = vignetteSequence.ComputeValueAt(time, tcs);
        state.LensDistortion = lensDistortionSequence.ComputeValueAt(time);
        state.Grain = grainSequence.ComputeValueAt(time);
        state.Gradient = gradientSequence.ComputeValueAt(time, tcs);
        state.Glitch = glitchSequence.ComputeValueAt(time);
        state.Hue = hueSequence.ComputeValueAt(time);
        return state;
    }

    private void OnEventsCameraPositionKeyframesChanged(object? sender, KeyframeList<EventKeyframe<Vector2>> e)
    {
        cameraPositionSequence.LoadKeyframes(EventHelper.ResolveGenericKeyframes(e));
    }

    private void OnEventsCameraRotationKeyframesChanged(object? sender, KeyframeList<EventKeyframe<float>> e)
    {
        cameraRotationSequence.LoadKeyframes(EventHelper.ResolveRotationKeyframes(e));
    }

    private void OnEventsCameraScaleKeyframesChanged(object? sender, KeyframeList<EventKeyframe<float>> e)
    {
        cameraScaleSequence.LoadKeyframes(EventHelper.ResolveGenericKeyframes(e));
    }
    
    private void OnEventsCameraShakeKeyframesChanged(object? sender, KeyframeList<EventKeyframe<float>> e)
    {
        cameraShakeSequence.LoadKeyframes(EventHelper.ResolveGenericKeyframes(e));
    }
    
    private void OnEventsChromaKeyframesChanged(object? sender, KeyframeList<EventKeyframe<float>> e)
    {
        chromaSequence.LoadKeyframes(EventHelper.ResolveGenericKeyframes(e));
    }
    
    private void OnBloomKeyframesChanged(object? sender, KeyframeList<EventKeyframe<BloomData>> e)
    {
        bloomSequence.LoadKeyframes(EventHelper.ResolveGenericKeyframes(e));
    }
    
    private void OnVignetteKeyframesChanged(object? sender, KeyframeList<EventKeyframe<VignetteData>> e)
    {
        vignetteSequence.LoadKeyframes(EventHelper.ResolveGenericKeyframes(e));
    }
    
    private void OnLensDistortionKeyframesChanged(object? sender, KeyframeList<EventKeyframe<LensDistortionData>> e)
    {
        lensDistortionSequence.LoadKeyframes(EventHelper.ResolveGenericKeyframes(e));
    }
    
    private void OnGrainKeyframesChanged(object? sender, KeyframeList<EventKeyframe<GrainData>> e)
    {
        grainSequence.LoadKeyframes(EventHelper.ResolveGenericKeyframes(e));
    }
    
    private void OnGradientKeyframesChanged(object? sender, KeyframeList<EventKeyframe<GradientData>> e)
    {
        gradientSequence.LoadKeyframes(EventHelper.ResolveGenericKeyframes(e));
    }
    
    private void OnGlitchKeyframesChanged(object? sender, KeyframeList<EventKeyframe<GlitchData>> e)
    {
        glitchSequence.LoadKeyframes(EventHelper.ResolveGenericKeyframes(e));
    }
    
    private void OnHueKeyframesChanged(object? sender, KeyframeList<EventKeyframe<float>> e)
    {
        hueSequence.LoadKeyframes(EventHelper.ResolveRotationKeyframes(e));
    }
}