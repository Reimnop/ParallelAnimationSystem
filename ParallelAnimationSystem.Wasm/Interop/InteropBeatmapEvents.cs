using System.Numerics;
using System.Runtime.InteropServices;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Model;
using ParallelAnimationSystem.Wasm.Interop.Data;
using BloomData = Pamx.Common.Data.BloomData;
using GlitchData = Pamx.Common.Data.GlitchData;
using GradientData = Pamx.Common.Data.GradientData;
using GrainData = Pamx.Common.Data.GrainData;
using LensDistortionData = Pamx.Common.Data.LensDistortionData;
using VignetteData = Pamx.Common.Data.VignetteData;

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
        var wrapper = new FixedSizeKeyframeListInteropWrapper<Keyframe<BloomData>>(keyframeList,
            new KeyframeInteropCodec<BloomData>());
        return InteropHelper.ObjectToIntPtr(wrapper);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapEvents_getVignette")]
    public static IntPtr GetVignette(IntPtr ptr)
    {
        var events = InteropHelper.IntPtrToObject<BeatmapEvents>(ptr);
        var keyframeList = events.Vignette;
        var wrapper = new FixedSizeKeyframeListInteropWrapper<Keyframe<VignetteData>>(keyframeList,
            new KeyframeInteropCodec<VignetteData>());
        return InteropHelper.ObjectToIntPtr(wrapper);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapEvents_getLensDistortion")]
    public static IntPtr GetLensDistortion(IntPtr ptr)
    {
        var events = InteropHelper.IntPtrToObject<BeatmapEvents>(ptr);
        var keyframeList = events.LensDistortion;
        var wrapper = new FixedSizeKeyframeListInteropWrapper<Keyframe<LensDistortionData>>(keyframeList,
            new KeyframeInteropCodec<LensDistortionData>());
        return InteropHelper.ObjectToIntPtr(wrapper);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapEvents_getGrain")]
    public static IntPtr GetGrain(IntPtr ptr)
    {
        var events = InteropHelper.IntPtrToObject<BeatmapEvents>(ptr);
        var keyframeList = events.Grain;
        var wrapper = new FixedSizeKeyframeListInteropWrapper<Keyframe<GrainData>>(keyframeList,
            new KeyframeInteropCodec<GrainData>());
        return InteropHelper.ObjectToIntPtr(wrapper);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapEvents_getGradient")]
    public static IntPtr GetGradient(IntPtr ptr)
    {
        var events = InteropHelper.IntPtrToObject<BeatmapEvents>(ptr);
        var keyframeList = events.Gradient;
        var wrapper = new FixedSizeKeyframeListInteropWrapper<Keyframe<GradientData>>(keyframeList,
            new KeyframeInteropCodec<GradientData>());
        return InteropHelper.ObjectToIntPtr(wrapper);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapEvents_getGlitch")]
    public static IntPtr GetGlitch(IntPtr ptr)
    {
        var events = InteropHelper.IntPtrToObject<BeatmapEvents>(ptr);
        var keyframeList = events.Glitch;
        var wrapper = new FixedSizeKeyframeListInteropWrapper<Keyframe<GlitchData>>(keyframeList,
            new KeyframeInteropCodec<GlitchData>());
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