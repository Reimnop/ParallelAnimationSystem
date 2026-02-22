using System.Numerics;
using ParallelAnimationSystem.Text;
using TmpIO;

namespace ParallelAnimationSystem.Rendering.OpenGLES;

public class RenderingFactory(IncomingResourceQueue queue) : IRenderingFactory
{
    private int currentMeshId;
    private int currentFontId;

    public IMesh CreateMesh(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<int> indices)
    {
        var mesh = new Mesh(currentMeshId++);
        queue.EnqueueMesh(new IncomingResourceQueue.IncomingMesh(mesh.Id, vertices.ToArray(), indices.ToArray()));
        return mesh;
    }

    public IFont CreateFont(Stream stream)
    {
        var fontFile = TmpRead.Read(stream);
        var font = new Font(currentFontId++, fontFile);
        queue.EnqueueFont(new IncomingResourceQueue.IncomingFont(font.Id, fontFile.Atlas));
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