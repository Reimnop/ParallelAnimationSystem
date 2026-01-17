using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using TmpIO;

namespace ParallelAnimationSystem.Rendering.OpenGL;

public class IncomingResourceQueue
{
    public record IncomingMesh(int MeshId, Vector2[] Vertices, int[] Indices);
    public record IncomingFont(int FontId, TmpAtlas Atlas);
    
    private readonly Queue<IncomingMesh> meshQueue = new();
    private readonly Queue<IncomingFont> fontQueue = new();
    
    public void EnqueueMesh(IncomingMesh mesh)
    {
        lock (meshQueue)
        {
            meshQueue.Enqueue(mesh);
        }
    }
    
    public void EnqueueFont(IncomingFont font)
    {
        lock (fontQueue)
        {
            fontQueue.Enqueue(font);
        }
    }
    
    public bool TryDequeueAllMeshes([MaybeNullWhen(false)] out List<IncomingMesh> meshes)
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
    
    public bool TryDequeueAllFonts([MaybeNullWhen(false)] out List<IncomingFont> fonts)
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