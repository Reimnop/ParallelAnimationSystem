using System.Runtime.InteropServices;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropKeyframe
{
    [UnmanagedCallersOnly(EntryPoint = "keyframe_getType")]
    public static int GetType(IntPtr ptr)
    {
        var keyframe = InteropHelper.IntPtrToObject<IKeyframe>(ptr);
        return (int)InteropKeyframeHelper.GetKeyframeType(keyframe);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "keyframe_getTime")]
    public static float GetTime(IntPtr ptr)
    {
        var keyframe = InteropHelper.IntPtrToObject<IKeyframe>(ptr);
        return keyframe.Time;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "keyframe_getEase")]
    public static int GetEase(IntPtr ptr)
    {
        var keyframe = InteropHelper.IntPtrToObject<IKeyframe>(ptr);
        return (int)keyframe.Ease;
    }
}