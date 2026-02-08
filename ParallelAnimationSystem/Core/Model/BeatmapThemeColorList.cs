using System.Collections;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Model;

public class BeatmapThemeColorUpdatedEventArgs(int index, ColorRgb newColor)
{
    public int Index => index;
    public ColorRgb NewColor => newColor;
}

public class BeatmapThemeColorList(int count) : IReadOnlyList<ColorRgb>
{
    public event EventHandler<BeatmapThemeColorUpdatedEventArgs>? Updated; 
    
    public int Count => count;
    
    private readonly ColorRgb[] colors = new ColorRgb[count];

    public ColorRgb this[int index]
    {
        get => colors[index];
        set
        {
            if (colors[index] == value)
                return;
            
            colors[index] = value;
            Updated?.Invoke(this, new BeatmapThemeColorUpdatedEventArgs(index, value));
        }
    }
    
    public IEnumerator<ColorRgb> GetEnumerator()
    {
        return ((IEnumerable<ColorRgb>)colors).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}