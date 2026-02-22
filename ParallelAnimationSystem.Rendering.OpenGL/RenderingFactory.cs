using System.Numerics;
using ParallelAnimationSystem.Text;
using TmpIO;

namespace ParallelAnimationSystem.Rendering.OpenGL;

public class RenderingFactory : IRenderingFactory
{
    public event EventHandler<Mesh>? MeshCreated;
    public event EventHandler<Font>? FontCreated;
    
    public IReadOnlyList<Mesh> Meshes => meshes;
    public IReadOnlyList<Font> Fonts => fonts;

    private readonly List<Mesh> meshes = [];
    private readonly List<Font> fonts = [];

    public IMesh CreateMesh(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<int> indices)
    {
        var mesh = new Mesh(meshes.Count, vertices.ToArray(), indices.ToArray());
        meshes.Add(mesh);
        MeshCreated?.Invoke(this, mesh);
        return mesh;
    }

    public IFont CreateFont(Stream stream)
    {
        var fontFile = TmpRead.Read(stream);
        var font = new Font(fonts.Count, fontFile);
        fonts.Add(font);
        FontCreated?.Invoke(this, font);
        return font;
    }
    
    public IText CreateText(ShapedRichText richText)
    {
        var renderGlyphs = richText.Glyphs.Select(x => new RenderGlyph
        {
            Min = x.Min,
            Max = x.Max,
            MinUV = x.MinUV,
            MaxUV = x.MaxUV,
            Color = x.Color,
            BoldItalic = x.BoldItalic,
            FontIndex = x.FontId
        });
        
        return new Text(renderGlyphs.ToArray());
    }

    public IDrawList CreateDrawList()
        => new DrawList();
}