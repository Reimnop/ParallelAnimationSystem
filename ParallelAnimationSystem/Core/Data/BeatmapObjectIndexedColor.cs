using System.Runtime.InteropServices;

namespace ParallelAnimationSystem.Core.Data;

[StructLayout(LayoutKind.Sequential)]
public struct BeatmapObjectIndexedColor
{
    public int ColorIndex1;
    public int ColorIndex2;
    public float Opacity;
}