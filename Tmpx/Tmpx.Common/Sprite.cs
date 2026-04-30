namespace Tmpx.Common;

public class Sprite : ISprite
{
    public float AdvanceWidth { get; set; }
    public float Ascender { get; set; }
    public float Descender { get; set; }
    public List<int> ShapeEntryIndices { get; set; } = [];
    IReadOnlyList<int> ISprite.ShapeEntryIndices => ShapeEntryIndices;
}