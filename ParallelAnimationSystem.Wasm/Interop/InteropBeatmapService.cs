using System.Runtime.InteropServices;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Service;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropBeatmapService
{
    [UnmanagedCallersOnly(EntryPoint = "beatmapService_getBeatmapData")]
    public static IntPtr GetBeatmapData(IntPtr ptr)
    {
        var beatmapService = InteropHelper.IntPtrToObject<BeatmapService>(ptr);
        return InteropHelper.ObjectToIntPtr(beatmapService.BeatmapData);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "beatmapService_getBeatmapFormat")]
    public static int GetBeatmapFormat(IntPtr ptr)
    {
        var beatmapService = InteropHelper.IntPtrToObject<BeatmapService>(ptr);
        return (int)beatmapService.BeatmapFormat;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "beatmapService_loadBeatmap")]
    public static void LoadBeatmap(IntPtr ptr, IntPtr beatmapDataPtr, int beatmapFormat)
    {
        var beatmapService = InteropHelper.IntPtrToObject<BeatmapService>(ptr);
        var beatmapData = Marshal.PtrToStringUTF8(beatmapDataPtr);
        if (beatmapData is null)
            throw new NullReferenceException("Beatmap data is null");
        
        beatmapService.LoadBeatmap(beatmapData, (BeatmapFormat)beatmapFormat);
    }
}