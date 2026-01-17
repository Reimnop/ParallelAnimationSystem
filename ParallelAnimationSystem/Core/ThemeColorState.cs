using System.Runtime.CompilerServices;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core;

public struct ThemeColorState
{
    public const int Buffer4Length = 4;
    public const int Buffer9Length = 9;
    
    [InlineArray(Buffer4Length)]
    public struct Buffer4<T>
    {
        public int Length => Buffer4Length;
        
        private T element0;
    }
    
    [InlineArray(Buffer9Length)]
    public struct Buffer9<T>
    {
        public int Length => Buffer9Length;
        
        private T element0;
    }

    public Buffer4<ColorRgb> Player;
    public Buffer9<ColorRgb> Object;
    public Buffer9<ColorRgb> Effect;
    public Buffer9<ColorRgb> ParallaxObject;
    public required ColorRgb Background;
    public required ColorRgb Gui;
    public required ColorRgb GuiAccent;
}