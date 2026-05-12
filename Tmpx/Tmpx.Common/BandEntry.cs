using System.Runtime.InteropServices;

namespace Tmpx.Common;

[StructLayout(LayoutKind.Sequential)]
public struct BandEntry
{
    public int CurveIndexBaseIndex;
    public int CurveIndexCount;
}