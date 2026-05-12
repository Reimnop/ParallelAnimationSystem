using System.Numerics;
using ParallelAnimationSystem.Core.Text;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering.Handle;
using Tmpx.Common;

namespace ParallelAnimationSystem.Rendering.Common;

public class RenderingFactory : IRenderingFactory
{
    public event EventHandler? FontBuffersUpdated;
    
    public ObservableSparseSet<Mesh> Meshes { get; } = new();
    public ObservableSparseSet<Text> Texts { get; } = new();

    // Global font buffers
    public QuadraticCurve[] FontCurves { get; private set; } = [];
    public int[] FontCurveIndices { get; private set; } = [];
    public BandEntry[] FontBandEntries { get; private set; } = [];
    public ShapeEntry[] FontShapeEntries { get; private set; } = [];

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
    
    public void SetFontBuffers(
        ReadOnlySpan<QuadraticCurve> curves,
        ReadOnlySpan<int> curveIndices,
        ReadOnlySpan<BandEntry> bandEntries,
        ReadOnlySpan<ShapeEntry> shapeEntries)
    {
        FontCurves = curves.ToArray();
        FontCurveIndices = curveIndices.ToArray();
        FontBandEntries = bandEntries.ToArray();
        FontShapeEntries = shapeEntries.ToArray();
        FontBuffersUpdated?.Invoke(this, EventArgs.Empty);
    }

    public TextHandle CreateText(ShapedRichText richText)
    {
        var renderGlyphs = richText.Glyphs.Select(x => new RenderGlyph
        {
            Transform = x.Transform,
            Color = x.Color,
            ShapeEntryIndex = x.ShapeEntryIndex,
        }).ToArray();
        var text = new Text(renderGlyphs);
        var id = Texts.Insert(text);
        return new TextHandle(id);
    }

    public void DestroyText(TextHandle handle)
    {
        if (!Texts.Remove(handle.Id, out _))
            throw new ArgumentException($"Invalid text handle '{handle.Id}'", nameof(handle));
    }
}
