using System.Runtime.InteropServices;
using ParallelAnimationSystem.Core.Model;
using ParallelAnimationSystem.Wasm.Interop.Data;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropBeatmapData
{
    [UnmanagedCallersOnly(EntryPoint = "beatmapData_getObjects")]
    public static IntPtr GetObjects(IntPtr ptr)
    {
        var beatmapData = InteropHelper.IntPtrToObject<BeatmapData>(ptr);
        var objects = beatmapData.Objects;
        var wrapper = new IdContainerInteropWrapper<BeatmapObject>(objects);
        return InteropHelper.ObjectToIntPtr(wrapper);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapData_getPrefabInstances")]
    public static IntPtr GetPrefabInstances(IntPtr ptr)
    {
        var beatmapData = InteropHelper.IntPtrToObject<BeatmapData>(ptr);
        var prefabInstances = beatmapData.PrefabInstances;
        var wrapper = new IdContainerInteropWrapper<BeatmapPrefabInstance>(prefabInstances);
        return InteropHelper.ObjectToIntPtr(wrapper);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapData_getPrefabs")]
    public static IntPtr GetPrefabs(IntPtr ptr)
    {
        var beatmapData = InteropHelper.IntPtrToObject<BeatmapData>(ptr);
        var prefabs = beatmapData.Prefabs;
        var wrapper = new IdContainerInteropWrapper<BeatmapPrefab>(prefabs);
        return InteropHelper.ObjectToIntPtr(wrapper);
    }
}