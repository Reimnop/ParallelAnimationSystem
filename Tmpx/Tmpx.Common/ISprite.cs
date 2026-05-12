namespace Tmpx.Common;

public interface ISprite
{
    float AdvanceWidth { get; }
    float Ascender { get; }
    float Descender { get; }
    IReadOnlyList<int> ShapeEntryIndices { get; }
}