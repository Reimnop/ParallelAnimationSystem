using System.Numerics;
using System.Runtime.InteropServices;
using Pamx.Events;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Wasm.Interop.Data;
using BeatmapEvents = ParallelAnimationSystem.Core.Model.BeatmapEvents;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropBeatmapEvents
{
    [UnmanagedCallersOnly(EntryPoint = "beatmapEvents_getCameraPosition")]
    public static IntPtr GetCameraPosition(IntPtr ptr)
    {
        var events = InteropHelper.IntPtrToObject<BeatmapEvents>(ptr);
        var keyframeList = events.CameraPosition;
        var wrapper = new FixedSizeKeyframeListInteropWrapper<Keyframe<Vector2>>(keyframeList,
            new KeyframeInteropCodec<Vector2>());
        return InteropHelper.ObjectToIntPtr(wrapper);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "beatmapEvents_getCameraScale")]
    public static IntPtr GetCameraScale(IntPtr ptr)
    {
        var events = InteropHelper.IntPtrToObject<BeatmapEvents>(ptr);
        var keyframeList = events.CameraScale;
        var wrapper = new FixedSizeKeyframeListInteropWrapper<Keyframe<float>>(keyframeList,
            new KeyframeInteropCodec<float>());
        return InteropHelper.ObjectToIntPtr(wrapper);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapEvents_getCameraRotation")]
    public static IntPtr GetCameraRotation(IntPtr ptr)
    {
        var events = InteropHelper.IntPtrToObject<BeatmapEvents>(ptr);
        var keyframeList = events.CameraRotation;
        var wrapper = new FixedSizeKeyframeListInteropWrapper<Keyframe<float>>(keyframeList,
            new KeyframeInteropCodec<float>());
        return InteropHelper.ObjectToIntPtr(wrapper);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "beatmapEvents_getCameraShake")]
    public static IntPtr GetCameraShake(IntPtr ptr)
    {
        var events = InteropHelper.IntPtrToObject<BeatmapEvents>(ptr);
        var keyframeList = events.CameraShake;
        var wrapper = new FixedSizeKeyframeListInteropWrapper<Keyframe<float>>(keyframeList,
            new KeyframeInteropCodec<float>());
        return InteropHelper.ObjectToIntPtr(wrapper);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapEvents_getTheme")]
    public static IntPtr GetTheme(IntPtr ptr)
    {
        var events = InteropHelper.IntPtrToObject<BeatmapEvents>(ptr);
        var keyframeList = events.Theme;
        var wrapper = new DynamicSizeKeyframeListInteropWrapper<Keyframe<string>>(keyframeList,
            new StringKeyframeInteropCodec());
        return InteropHelper.ObjectToIntPtr(wrapper);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapEvents_getChroma")]
    public static IntPtr GetChroma(IntPtr ptr)
    {
        var events = InteropHelper.IntPtrToObject<BeatmapEvents>(ptr);
        var keyframeList = events.Chroma;
        var wrapper = new FixedSizeKeyframeListInteropWrapper<Keyframe<float>>(keyframeList,
            new KeyframeInteropCodec<float>());
        return InteropHelper.ObjectToIntPtr(wrapper);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapEvents_getBloom")]
    public static IntPtr GetBloom(IntPtr ptr)
    {
        var events = InteropHelper.IntPtrToObject<BeatmapEvents>(ptr);
        var keyframeList = events.Bloom;
        var wrapper = new FixedSizeKeyframeListInteropWrapper<Keyframe<BloomValue>>(keyframeList,
            new KeyframeInteropCodec<BloomValue>());
        return InteropHelper.ObjectToIntPtr(wrapper);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapEvents_getVignette")]
    public static IntPtr GetVignette(IntPtr ptr)
    {
        var events = InteropHelper.IntPtrToObject<BeatmapEvents>(ptr);
        var keyframeList = events.Vignette;
        var wrapper = new FixedSizeKeyframeListInteropWrapper<Keyframe<VignetteValue>>(keyframeList,
            new KeyframeInteropCodec<VignetteValue>());
        return InteropHelper.ObjectToIntPtr(wrapper);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapEvents_getLensDistortion")]
    public static IntPtr GetLensDistortion(IntPtr ptr)
    {
        var events = InteropHelper.IntPtrToObject<BeatmapEvents>(ptr);
        var keyframeList = events.LensDistortion;
        var wrapper = new FixedSizeKeyframeListInteropWrapper<Keyframe<LensDistortionValue>>(keyframeList,
            new KeyframeInteropCodec<LensDistortionValue>());
        return InteropHelper.ObjectToIntPtr(wrapper);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapEvents_getGrain")]
    public static IntPtr GetGrain(IntPtr ptr)
    {
        var events = InteropHelper.IntPtrToObject<BeatmapEvents>(ptr);
        var keyframeList = events.Grain;
        var wrapper = new FixedSizeKeyframeListInteropWrapper<Keyframe<GrainValue>>(keyframeList,
            new KeyframeInteropCodec<GrainValue>());
        return InteropHelper.ObjectToIntPtr(wrapper);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapEvents_getGradient")]
    public static IntPtr GetGradient(IntPtr ptr)
    {
        var events = InteropHelper.IntPtrToObject<BeatmapEvents>(ptr);
        var keyframeList = events.Gradient;
        var wrapper = new FixedSizeKeyframeListInteropWrapper<Keyframe<GradientValue>>(keyframeList,
            new KeyframeInteropCodec<GradientValue>());
        return InteropHelper.ObjectToIntPtr(wrapper);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapEvents_getGlitch")]
    public static IntPtr GetGlitch(IntPtr ptr)
    {
        var events = InteropHelper.IntPtrToObject<BeatmapEvents>(ptr);
        var keyframeList = events.Glitch;
        var wrapper = new FixedSizeKeyframeListInteropWrapper<Keyframe<GlitchValue>>(keyframeList,
            new KeyframeInteropCodec<GlitchValue>());
        return InteropHelper.ObjectToIntPtr(wrapper);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapEvents_getHue")]
    public static IntPtr GetHue(IntPtr ptr)
    {
        var events = InteropHelper.IntPtrToObject<BeatmapEvents>(ptr);
        var keyframeList = events.Hue;
        var wrapper = new FixedSizeKeyframeListInteropWrapper<Keyframe<float>>(keyframeList,
            new KeyframeInteropCodec<float>());
        return InteropHelper.ObjectToIntPtr(wrapper);
    }
}