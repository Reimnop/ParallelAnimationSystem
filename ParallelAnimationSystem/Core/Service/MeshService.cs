using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Rendering.Handle;

namespace ParallelAnimationSystem.Core.Service;

public class MeshService : IDisposable
{
    private readonly IRenderQueue renderQueue;
    private readonly List<List<MeshHandle>> meshes = [];

    public MeshService(IRenderQueue renderQueue, ILogger<MeshService> logger)
    {
        this.renderQueue = renderQueue;
        
        logger.LogInformation("Registering meshes");
        
        meshes.Add([
            renderQueue.CreateMesh(PaAssets.SquareFilledVertices, PaAssets.SquareFilledIndices),
            renderQueue.CreateMesh(PaAssets.SquareOutlineVertices, PaAssets.SquareOutlineIndices),
            renderQueue.CreateMesh(PaAssets.SquareOutlineThinVertices, PaAssets.SquareOutlineThinIndices),
        ]);

        meshes.Add([
            renderQueue.CreateMesh(PaAssets.CircleFilledVertices, PaAssets.CircleFilledIndices),
            renderQueue.CreateMesh(PaAssets.CircleOutlineVertices, PaAssets.CircleOutlineIndices),
            renderQueue.CreateMesh(PaAssets.CircleHalfVertices, PaAssets.CircleHalfIndices),
            renderQueue.CreateMesh(PaAssets.CircleHalfOutlineVertices, PaAssets.CircleHalfOutlineIndices),
            renderQueue.CreateMesh(PaAssets.CircleOutlineThinVertices, PaAssets.CircleOutlineThinIndices),
            renderQueue.CreateMesh(PaAssets.CircleQuarterVertices, PaAssets.CircleQuarterIndices),
            renderQueue.CreateMesh(PaAssets.CircleQuarterOutlineVertices, PaAssets.CircleQuarterOutlineIndices),
            renderQueue.CreateMesh(PaAssets.CircleHalfQuarterVertices, PaAssets.CircleHalfQuarterIndices),
            renderQueue.CreateMesh(PaAssets.CircleHalfQuarterOutlineVertices, PaAssets.CircleHalfQuarterOutlineIndices),
        ]);

        meshes.Add([
            renderQueue.CreateMesh(PaAssets.TriangleFilledVertices, PaAssets.TriangleFilledIndices),
            renderQueue.CreateMesh(PaAssets.TriangleOutlineVertices, PaAssets.TriangleOutlineIndices),
            renderQueue.CreateMesh(PaAssets.TriangleRightFilledVertices, PaAssets.TriangleRightFilledIndices),
            renderQueue.CreateMesh(PaAssets.TriangleRightOutlineVertices, PaAssets.TriangleRightOutlineIndices),
            renderQueue.CreateMesh(PaAssets.TriangleFilledBottomOriginVertices, PaAssets.TriangleFilledBottomOriginIndices),
            renderQueue.CreateMesh(PaAssets.TriangleOutlineBottomOriginVertices, PaAssets.TriangleOutlineBottomOriginIndices),
        ]);

        meshes.Add([
            renderQueue.CreateMesh(PaAssets.ArrowVertices, PaAssets.ArrowIndices),
            renderQueue.CreateMesh(PaAssets.ArrowHeadVertices, PaAssets.ArrowHeadIndices),
        ]);

        meshes.Add([]);

        meshes.Add([
            renderQueue.CreateMesh(PaAssets.HexagonFilledVertices, PaAssets.HexagonFilledIndices),
            renderQueue.CreateMesh(PaAssets.HexagonOutlineVertices, PaAssets.HexagonOutlineIndices),
            renderQueue.CreateMesh(PaAssets.HexagonOutlineThinVertices, PaAssets.HexagonOutlineThinIndices),
            renderQueue.CreateMesh(PaAssets.HexagonHalfVertices, PaAssets.HexagonHalfIndices),
            renderQueue.CreateMesh(PaAssets.HexagonHalfOutlineVertices, PaAssets.HexagonHalfOutlineIndices),
            renderQueue.CreateMesh(PaAssets.HexagonHalfOutlineThinVertices, PaAssets.HexagonHalfOutlineThinIndices),
        ]);
    }

    public bool TryGetMeshForShape(int shape, int shapeOption, out MeshHandle mesh)
    {
        if (shape < meshes.Count && shapeOption < meshes[shape].Count)
        {
            mesh = meshes[shape][shapeOption];
            return true;
        }

        mesh = default;
        return false;
    }

    public void Dispose()
    {
        foreach (var meshList in meshes)
        {
            foreach (var mesh in meshList)
            {
                renderQueue.DestroyMesh(mesh);
            }
        }
    }
}