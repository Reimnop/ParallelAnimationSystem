using System.Numerics;
using ParallelAnimationSystem.Core.Text;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering.Handle;

namespace ParallelAnimationSystem.Rendering.Common;

public class RenderingFactory : IRenderingFactory
{
    public ObservableSparseSet<Mesh> Meshes { get; } = new();
    public ObservableSparseSet<Font> Fonts { get; } = new();
    public ObservableSparseSet<Text> Texts { get; } = new();

    public MeshHandle CreateMesh(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<int> indices)
    {
        var mesh = new Mesh(vertices.ToArray(), indices.ToArray());
        var id = Meshes.Insert(mesh);
        return new MeshHandle(id);
    }

    public void DestroyMesh(MeshHandle handle)
    {
        if (!Meshes.Remove(handle.Id, out _))
            throw new ArgumentException($"Invalid mesh handle '{handle.Id}'", nameof(handle));
    }

    public FontHandle CreateFont(int width, int height, ReadOnlySpan<byte> atlas)
    {
        var expectedLength = width * height * 6; // 3 * float16
        if (atlas.Length != expectedLength)
            throw new ArgumentException($"Invalid atlas data size, expected {expectedLength}, actual {atlas.Length}", nameof(atlas));
        
        var dstAtlas = new byte[expectedLength];
        atlas.CopyTo(dstAtlas);
        
        var font = new Font(width, height, dstAtlas);
        var id = Fonts.Insert(font);
        return new FontHandle(id);
    }

    public void DestroyFont(FontHandle handle)
    {
        if (!Fonts.Remove(handle.Id, out _))
            throw new ArgumentException($"Invalid font handle '{handle.Id}'", nameof(handle));
    }

    public TextHandle CreateText(ShapedRichText richText)
    {
        var renderGlyphs = richText.Glyphs.Select(x => new RenderGlyph
        {
            Min = x.Min,
            Max = x.Max,
            MinUV = x.MinUV,
            MaxUV = x.MaxUV,
            Color = x.Color,
            Rotation = x.Rotation,
            BoldItalic = x.BoldItalic,
            AtlasIndex = x.Font?.Id ?? -1
        });
        var text = new Text(renderGlyphs.ToArray());
        var id = Texts.Insert(text);
        return new TextHandle(id);
    }
    
    public void DestroyText(TextHandle handle)
    {
        if (!Texts.Remove(handle.Id, out _))
            throw new ArgumentException($"Invalid text handle '{handle.Id}'", nameof(handle));
    }
}