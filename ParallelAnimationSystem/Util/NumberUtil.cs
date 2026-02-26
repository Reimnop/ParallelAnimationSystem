using System.Runtime.CompilerServices;

namespace ParallelAnimationSystem.Util;

public delegate ulong PseudoRng();

public static class NumberUtil
{
    public static PseudoRng CreatePseudoRng(ulong seed)
    {
        return () =>
        {
            seed += 0x9E3779B97F4A7C15UL;
            return SplitMix64(seed);
        };
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Mix(ulong a, ulong b)
    {
        var h = a + 0x9E3779B97F4A7C15UL;
        h ^= b;
        return SplitMix64(h);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong SplitMix64(ulong x)
    {
        x ^= x >> 30;
        x *= 0xBF58476D1CE4E5B9UL;
        x ^= x >> 27;
        x *= 0x94D049BB133111EBUL;
        x ^= x >> 31;
        return x;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float UlongToFloat01(ulong x)
        => (x >> 40) * (1f / 16777216f); // 2^24
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint FloatToOrderedUInt(float value)
    {
        var bits = BitConverter.SingleToUInt32Bits(value);
        var mask = (uint)((int)bits >> 31); // 0x00000000 or 0xFFFFFFFF
        return bits ^ (mask | 0x80000000);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort HalfToOrderedUShort(Half value)
    {
        var bits = BitConverter.HalfToUInt16Bits(value);
        var mask = (ushort)((short)bits >> 15); // 0x0000 or 0xFFFF
        return (ushort)(bits ^ (mask | 0x8000));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ComputeHash(string str)
    {
        // FNV-1a hash
        var h = 1469598103934665603UL;
        foreach (var c in str)
        {
            h ^= c;
            h *= 1099511628211UL;
        }

        return h;
    }
}