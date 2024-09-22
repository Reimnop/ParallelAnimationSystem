using System.Drawing;
using OpenTK.Mathematics;

namespace ParallelAnimationSystem.Util;

public static class ColorUtil
{
    public static Color4<Rgba> ToColor4(this Color color)
    {
        return new Color4<Rgba>(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
    }
}