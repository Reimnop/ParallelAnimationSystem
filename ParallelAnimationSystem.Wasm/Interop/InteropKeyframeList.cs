using System.Runtime.InteropServices;
using ParallelAnimationSystem.Wasm.Interop.Data;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropKeyframeList
{
    [UnmanagedCallersOnly(EntryPoint = "keyframeList_getCount")]
    public static int GetCount(IntPtr ptr)
    {
        var wrapper = InteropHelper.IntPtrToObject<IKeyframeListInteropWrapper>(ptr);
        return wrapper.Count;
    }

    [UnmanagedCallersOnly(EntryPoint = "keyframeList_getKeyframeSize")]
    public static int GetKeyframeSize(IntPtr ptr, int index)
    {
        var wrapper = InteropHelper.IntPtrToObject<IKeyframeListInteropWrapper>(ptr);
        return wrapper.GetKeyframeSize(index);
    }

    [UnmanagedCallersOnly(EntryPoint = "keyframeList_fetchAt")]
    public static void FetchAt(IntPtr ptr, IntPtr bufferPtr, int index)
    {
        var wrapper = InteropHelper.IntPtrToObject<IKeyframeListInteropWrapper>(ptr);
        wrapper.FetchAt(bufferPtr, index);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "keyframeList_getBufferSize")]
    public static int GetBufferSize(IntPtr ptr, int start, int count)
    {
        var wrapper = InteropHelper.IntPtrToObject<IKeyframeListInteropWrapper>(ptr);
        return wrapper.GetBufferSize(start, count);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "keyframeList_fetchRange")]
    public static void FetchRange(IntPtr ptr, IntPtr bufferPtr, int start, int count)
    {
        var wrapper = InteropHelper.IntPtrToObject<IKeyframeListInteropWrapper>(ptr);
        wrapper.FetchRange(bufferPtr, start, count);
    }

    [UnmanagedCallersOnly(EntryPoint = "keyframeList_load")]
    public static void Load(IntPtr ptr, IntPtr bufferPtr, int count)
    {
        var wrapper = InteropHelper.IntPtrToObject<IKeyframeListInteropWrapper>(ptr);
        wrapper.Load(bufferPtr, count);
    }
}