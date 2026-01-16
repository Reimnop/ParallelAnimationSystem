using System.Numerics;
using ParallelAnimationSystem.Text;
using TmpIO;

namespace ParallelAnimationSystem.Rendering.OpenGL;

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
    
    public IText CreateText(RichText text, FontCollection fonts)
    {
        var textShaper = new TextShaper<RenderGlyph>(
            (min, max, minUV, maxUV, color, boldItalic, fontIndex) 
                => new RenderGlyph(min, max, minUV, maxUV, color, boldItalic, fontIndex),
            fonts);
        
        var shapedText = textShaper.ShapeText(text);
        return new Text(shapedText.ToArray());
    }

    public IDrawList CreateDrawList()
        => new DrawList();
}