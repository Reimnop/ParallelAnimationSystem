using System.Drawing;
using System.Numerics;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Util;

public static class ColorUtil
{
    public static ColorRgba ToColorRgba(this Color color) 
        => new(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
    
    public static ColorRgb ToColorRgb(this Color color) 
        => new(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);
}