using System.Runtime.InteropServices;
using ParallelAnimationSystem.Core.Model;
using ParallelAnimationSystem.Wasm.Interop.Data;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropBeatmapPrefab
{
    [UnmanagedCallersOnly(EntryPoint = "beatmapPrefab_new")]
    public static IntPtr New(IntPtr idPtr)
    {
        var id = Marshal.PtrToStringUTF8(idPtr);
        if (id is null)
            return IntPtr.Zero;
        var prefab = new BeatmapPrefab(id);
        return InteropHelper.ObjectToIntPtr(prefab);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapPrefab_getId")]
    public static IntPtr GetId(IntPtr ptr)
    {
        var prefab = InteropHelper.IntPtrToObject<BeatmapPrefab>(ptr);
        return InteropHelper.ObjectToIntPtr(prefab.Id);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapPrefab_getName")]
    public static IntPtr GetName(IntPtr ptr)
    {
        var prefab = InteropHelper.IntPtrToObject<BeatmapPrefab>(ptr);
        return InteropHelper.ObjectToIntPtr(prefab.Name);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapPrefab_setName")]
    public static void SetName(IntPtr ptr, IntPtr namePtr)
    {
        var prefab = InteropHelper.IntPtrToObject<BeatmapPrefab>(ptr);
        var name = Marshal.PtrToStringUTF8(namePtr);
        if (name is null) 
            return;
        prefab.Name = name;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapPrefab_getOffset")]
    public static float GetOffset(IntPtr ptr)
    {
        var prefab = InteropHelper.IntPtrToObject<BeatmapPrefab>(ptr);
        return prefab.Offset;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapPrefab_setOffset")]
    public static void SetOffset(IntPtr ptr, float value)
    {
        var prefab = InteropHelper.IntPtrToObject<BeatmapPrefab>(ptr);
        prefab.Offset = value;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "beatmapPrefab_getObjects")]
    public static IntPtr GetObjects(IntPtr ptr)
    {        
        var prefab = InteropHelper.IntPtrToObject<BeatmapPrefab>(ptr);
        var wrapper = new IdContainerInteropWrapper<BeatmapObject>(prefab.Objects);
        return InteropHelper.ObjectToIntPtr(wrapper);
    }
}