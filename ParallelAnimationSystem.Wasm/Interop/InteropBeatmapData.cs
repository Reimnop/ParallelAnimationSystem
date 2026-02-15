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
        var adapter = new InteropIdContainerAdapter<BeatmapObject>(objects);
        return InteropHelper.ObjectToIntPtr(adapter);
    }
}