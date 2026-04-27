using System.Runtime.InteropServices;

namespace Tmpx.Common;

[StructLayout(LayoutKind.Sequential)]
public struct Glyph
{
    public GlyphBands Bands; 
    public GlyphMetrics Metrics;
    public GlyphColor Color;
}