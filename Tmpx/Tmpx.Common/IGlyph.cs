namespace Tmpx.Common;

public interface IGlyph
{
    float AdvanceWidth { get; }
    int ShapeEntryIndex { get; }
}