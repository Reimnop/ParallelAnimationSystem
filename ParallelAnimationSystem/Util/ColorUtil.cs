using System.Drawing;
using OpenTK.Mathematics;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Util;

public static class ColorUtil
{
    public static ColorRgba ToColorRgba(this Color color) 
        => new(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
}