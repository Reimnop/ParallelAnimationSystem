using System.Runtime.InteropServices;
using ParallelAnimationSystem.Core.Service;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropRandomSeedService
{
    [UnmanagedCallersOnly(EntryPoint = "randomSeedService_getSeed")]
    public static ulong GetSeed(IntPtr ptr)
    {
        var randomSeedService = InteropHelper.IntPtrToObject<RandomSeedService>(ptr);
        return randomSeedService.Seed;
    }

    [UnmanagedCallersOnly(EntryPoint = "randomSeedService_setSeed")]
    public static void SetSeed(IntPtr ptr, ulong seed)
    {
        var randomSeedService = InteropHelper.IntPtrToObject<RandomSeedService>(ptr);
        randomSeedService.Seed = seed;
    }
}