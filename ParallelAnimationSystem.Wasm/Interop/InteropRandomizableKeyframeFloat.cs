using System.Runtime.InteropServices;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropRandomizableKeyframeFloat
{
    [UnmanagedCallersOnly(EntryPoint = "randomizableKeyframe_float_new")]
    public static IntPtr New(
        float time, Ease ease, float value,
        RandomMode randomMode, float randomValue, float randomInterval,
        bool isRelative)
    {
        var keyframe = new RandomizableKeyframe<float>(
            time, ease, value,
            randomMode, randomValue, randomInterval,
            isRelative);
        return InteropHelper.ObjectToIntPtr(keyframe);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "randomizableKeyframe_float_getValue")]
    public static float GetValue(IntPtr ptr)
    {
        var keyframe = InteropHelper.IntPtrToObject<RandomizableKeyframe<float>>(ptr);
        return keyframe.Value;
    }

    [UnmanagedCallersOnly(EntryPoint = "randomizableKeyframe_float_getRandomMode")]
    public static int GetRandomMode(IntPtr ptr)
    {
        var keyframe = InteropHelper.IntPtrToObject<RandomizableKeyframe<float>>(ptr);
        return (int)keyframe.RandomMode;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "randomizableKeyframe_float_getRandomValue")]
    public static float GetRandomValue(IntPtr ptr)
    {
        var keyframe = InteropHelper.IntPtrToObject<RandomizableKeyframe<float>>(ptr);
        return keyframe.RandomValue;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "randomizableKeyframe_float_getRandomInterval")]
    public static float GetRandomInterval(IntPtr ptr)
    {
        var keyframe = InteropHelper.IntPtrToObject<RandomizableKeyframe<float>>(ptr);
        return keyframe.RandomInterval;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "randomizableKeyframe_float_getIsRelative")]
    public static bool GetIsRelative(IntPtr ptr)
    {
        var keyframe = InteropHelper.IntPtrToObject<RandomizableKeyframe<float>>(ptr);
        return keyframe.IsRelative;
    }
}