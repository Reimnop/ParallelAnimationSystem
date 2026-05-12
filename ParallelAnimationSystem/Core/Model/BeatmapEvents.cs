using System.Numerics;
using Pamx.Events;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Model;

public class BeatmapEvents
{
    public event EventHandler<KeyframeList<Keyframe<Vector2>>>? CameraPositionKeyframesChanged;
    public event EventHandler<KeyframeList<Keyframe<float>>>? CameraScaleKeyframesChanged;
    public event EventHandler<KeyframeList<Keyframe<float>>>? CameraRotationKeyframesChanged;
    public event EventHandler<KeyframeList<Keyframe<float>>>? CameraShakeKeyframesChanged;
    public event EventHandler<KeyframeList<Keyframe<string>>>? ThemeKeyframesChanged;
    public event EventHandler<KeyframeList<Keyframe<float>>>? ChromaKeyframesChanged;
    public event EventHandler<KeyframeList<Keyframe<BloomValue>>>? BloomKeyframesChanged;
    public event EventHandler<KeyframeList<Keyframe<VignetteValue>>>? VignetteKeyframesChanged;
    public event EventHandler<KeyframeList<Keyframe<LensDistortionValue>>>? LensDistortionKeyframesChanged;
    public event EventHandler<KeyframeList<Keyframe<GrainValue>>>? GrainKeyframesChanged;
    public event EventHandler<KeyframeList<Keyframe<GradientValue>>>? GradientKeyframesChanged;
    public event EventHandler<KeyframeList<Keyframe<GlitchValue>>>? GlitchKeyframesChanged;
    public event EventHandler<KeyframeList<Keyframe<float>>>? HueKeyframesChanged;
    
    public KeyframeList<Keyframe<Vector2>> CameraPosition { get; } = [];
    public KeyframeList<Keyframe<float>> CameraScale { get; } = [];
    public KeyframeList<Keyframe<float>> CameraRotation { get; } = [];
    public KeyframeList<Keyframe<float>> CameraShake { get; } = [];
    public KeyframeList<Keyframe<string>> Theme { get; } = [];
    public KeyframeList<Keyframe<float>> Chroma { get; } = [];
    public KeyframeList<Keyframe<BloomValue>> Bloom { get; } = [];
    public KeyframeList<Keyframe<VignetteValue>> Vignette { get; } = [];
    public KeyframeList<Keyframe<LensDistortionValue>> LensDistortion { get; } = [];
    public KeyframeList<Keyframe<GrainValue>> Grain { get; } = [];
    public KeyframeList<Keyframe<GradientValue>> Gradient { get; } = [];
    public KeyframeList<Keyframe<GlitchValue>> Glitch { get; } = [];
    public KeyframeList<Keyframe<float>> Hue { get; } = [];

    public BeatmapEvents()
    {
        CameraPosition.ListChanged += (_, e) => CameraPositionKeyframesChanged?.Invoke(this, e);
        CameraScale.ListChanged += (_, e) => CameraScaleKeyframesChanged?.Invoke(this, e);
        CameraRotation.ListChanged += (_, e) => CameraRotationKeyframesChanged?.Invoke(this, e);
        CameraShake.ListChanged += (_, e) => CameraShakeKeyframesChanged?.Invoke(this, e);
        Theme.ListChanged += (_, e) => ThemeKeyframesChanged?.Invoke(this, e);
        Chroma.ListChanged += (_, e) => ChromaKeyframesChanged?.Invoke(this, e);
        Bloom.ListChanged += (_, e) => BloomKeyframesChanged?.Invoke(this, e);
        Vignette.ListChanged += (_, e) => VignetteKeyframesChanged?.Invoke(this, e);
        LensDistortion.ListChanged += (_, e) => LensDistortionKeyframesChanged?.Invoke(this, e);
        Grain.ListChanged += (_, e) => GrainKeyframesChanged?.Invoke(this, e);
        Gradient.ListChanged += (_, e) => GradientKeyframesChanged?.Invoke(this, e);
        Glitch.ListChanged += (_, e) => GlitchKeyframesChanged?.Invoke(this, e);
        Hue.ListChanged += (_, e) => HueKeyframesChanged?.Invoke(this, e);
    }

    public void Clear()
    {
        CameraPosition.Clear();
        CameraScale.Clear();
        CameraRotation.Clear();
        CameraShake.Clear();
        Theme.Clear();
        Chroma.Clear();
        Bloom.Clear();
        Vignette.Clear();
        LensDistortion.Clear();
        Grain.Clear();
        Gradient.Clear();
        Glitch.Clear();
        Hue.Clear();
    }
}