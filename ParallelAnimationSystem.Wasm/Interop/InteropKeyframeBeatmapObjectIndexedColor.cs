using System.Runtime.InteropServices;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropKeyframeBeatmapObjectIndexedColor
{
    [StructLayout(LayoutKind.Sequential)]
    private struct InteropBeatmapObjectIndexedColor
    {
        public int ColorIndex1;
        public int ColorIndex2;
        public float Opacity;

        public BeatmapObjectIndexedColor To()
            => new()
            {
                ColorIndex1 = ColorIndex1,
                ColorIndex2 = ColorIndex2,
                Opacity = Opacity
            };

        public static InteropBeatmapObjectIndexedColor From(BeatmapObjectIndexedColor v)
            => new()
            {
                ColorIndex1 = v.ColorIndex1,
                ColorIndex2 = v.ColorIndex2,
                Opacity = v.Opacity
            };
    }
    
    [UnmanagedCallersOnly(EntryPoint = "keyframe_beatmapObjectIndexedColor_new")]
    public static IntPtr New(float time, Ease ease, IntPtr valuePtr)
    {
        var value = Marshal.PtrToStructure<InteropBeatmapObjectIndexedColor>(valuePtr);
        var keyframe = new Keyframe<BeatmapObjectIndexedColor>(time, ease, value.To());
        return InteropHelper.ObjectToIntPtr(keyframe);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "keyframe_beatmapObjectIndexedColor_getValue")]
    public static void GetValue(IntPtr ptr, IntPtr valuePtr)
    {
        var keyframe = InteropHelper.IntPtrToObject<Keyframe<BeatmapObjectIndexedColor>>(ptr);
        Marshal.StructureToPtr(keyframe, valuePtr, false);
    }
}