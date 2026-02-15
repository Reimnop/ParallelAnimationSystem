using System.Runtime.InteropServices;
using ParallelAnimationSystem.Core.Model;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropBeatmapObject
{
    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_new")]
    public static IntPtr New(IntPtr idPtr)
    {
        var id = Marshal.PtrToStringUTF8(idPtr);
        if (id is null)
            return IntPtr.Zero;
        var beatmapObject = new BeatmapObject(id);
        return InteropHelper.ObjectToIntPtr(beatmapObject);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_getId")]
    public static IntPtr GetId(IntPtr ptr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        var id = beatmapObject.Id;
        return InteropHelper.StringToIntPtrUTF8(id);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_getName")]
    public static IntPtr GetName(IntPtr ptr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        var name = beatmapObject.Name;
        return InteropHelper.StringToIntPtrUTF8(name);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_setName")]
    public static void SetName(IntPtr ptr, IntPtr namePtr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        var name = Marshal.PtrToStringUTF8(namePtr);
        if (name is null)
            return;
        beatmapObject.Name = name;
    }
}