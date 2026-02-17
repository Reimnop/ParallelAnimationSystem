using System.Numerics;
using System.Runtime.InteropServices;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropRandomizableKeyframeVector2 
{
    [UnmanagedCallersOnly(EntryPoint = "randomizableKeyframe_vector2_new")]
    public static unsafe IntPtr New(
        float time, Ease ease, float* valuePtr,
        RandomMode randomMode, float* randomValuePtr, float randomInterval,
        bool isRelative)
    {
        var value = new Vector2(valuePtr[0], valuePtr[1]);
        var randomValue = new Vector2(randomValuePtr[0], randomValuePtr[1]);
        var keyframe = new RandomizableKeyframe<Vector2>(
            time, ease, value,
            randomMode, randomValue, randomInterval,
            isRelative);
        return InteropHelper.ObjectToIntPtr(keyframe);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "randomizableKeyframe_vector2_getValue")]
    public static unsafe void GetValue(IntPtr ptr, float* valuePtr)
    {
        var keyframe = InteropHelper.IntPtrToObject<RandomizableKeyframe<Vector2>>(ptr);
        valuePtr[0] = keyframe.Value.X;
        valuePtr[1] = keyframe.Value.Y;
    }

    [UnmanagedCallersOnly(EntryPoint = "randomizableKeyframe_vector2_getRandomMode")]
    public static int GetRandomMode(IntPtr ptr)
    {
        var keyframe = InteropHelper.IntPtrToObject<RandomizableKeyframe<Vector2>>(ptr);
        return (int)keyframe.RandomMode;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "randomizableKeyframe_vector2_getRandomValue")]
    public static unsafe void GetRandomValue(IntPtr ptr, float* randomValuePtr)
    {
        var keyframe = InteropHelper.IntPtrToObject<RandomizableKeyframe<Vector2>>(ptr);
        randomValuePtr[0] = keyframe.RandomValue.X;
        randomValuePtr[1] = keyframe.RandomValue.Y;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "randomizableKeyframe_vector2_getRandomInterval")]
    public static float GetRandomInterval(IntPtr ptr)
    {
        var keyframe = InteropHelper.IntPtrToObject<RandomizableKeyframe<Vector2>>(ptr);
        return keyframe.RandomInterval;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "randomizableKeyframe_vector2_getIsRelative")]
    public static bool GetIsRelative(IntPtr ptr)
    {
        var keyframe = InteropHelper.IntPtrToObject<RandomizableKeyframe<Vector2>>(ptr);
        return keyframe.IsRelative;
    }
}