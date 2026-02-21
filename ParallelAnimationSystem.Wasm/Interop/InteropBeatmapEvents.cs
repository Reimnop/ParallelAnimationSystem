using System.Numerics;
using System.Runtime.InteropServices;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Model;
using ParallelAnimationSystem.Wasm.Interop.Data;

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
}