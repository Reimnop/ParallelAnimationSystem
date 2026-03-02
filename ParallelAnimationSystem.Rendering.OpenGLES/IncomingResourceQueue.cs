using System.Diagnostics.CodeAnalysis;

namespace ParallelAnimationSystem.Rendering.OpenGLES;

public class IncomingResourceQueue : IDisposable
{
    private readonly Queue<Mesh> meshQueue = new();
    private readonly Queue<Font> fontQueue = new();

    private readonly RenderingFactory renderingFactory;

    public IncomingResourceQueue(IRenderingFactory renderingFactory)
    {
        this.renderingFactory = (RenderingFactory)renderingFactory;
        
        // add all existing resources to the queues
        foreach (var mesh in this.renderingFactory.Meshes)
            meshQueue.Enqueue(mesh);
        
        foreach (var font in this.renderingFactory.Fonts)
            fontQueue.Enqueue(font);

        this.renderingFactory.MeshCreated += OnMeshCreated;
        this.renderingFactory.FontCreated += OnFontCreated;
    }

    public void Dispose()
    {
        renderingFactory.MeshCreated -= OnMeshCreated;
        renderingFactory.FontCreated -= OnFontCreated;
    }
    
    private void OnMeshCreated(object? sender, Mesh e)
    {
        lock (meshQueue)
            meshQueue.Enqueue(e);
    }
    
    private void OnFontCreated(object? sender, Font e)
    {
        lock (fontQueue)
            fontQueue.Enqueue(e);
    }
    
    public bool TryDequeueAllMeshes([MaybeNullWhen(false)] out List<Mesh> meshes)
    {
        lock (meshQueue)
        {
            if (meshQueue.Count == 0)
            {
                meshes = null;
                return false;
            }

            meshes = [];
            while (meshQueue.TryDequeue(out var mesh))
                meshes.Add(mesh);
        }
        
        return true;
    }
    
    public bool TryDequeueAllFonts([MaybeNullWhen(false)] out List<Font> fonts)
    {
        lock (fontQueue)
        {
            if (fontQueue.Count == 0)
            {
                fonts = null;
                return false;
            }

            fonts = [];
            while (fontQueue.TryDequeue(out var font))
                fonts.Add(font);
        }
        
        return true;
    }
}