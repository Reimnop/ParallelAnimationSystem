using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem.Core.Service;

public class MeshService
{
    private readonly List<List<IMesh>> meshes = [];

    public MeshService(IRenderingFactory renderingFactory, ILogger<MeshService> logger)
    {
        logger.LogInformation("Registering meshes");
        
        meshes.Add([
            renderingFactory.CreateMesh(PaAssets.SquareFilledVertices, PaAssets.SquareFilledIndices),
            renderingFactory.CreateMesh(PaAssets.SquareOutlineVertices, PaAssets.SquareOutlineIndices),
            renderingFactory.CreateMesh(PaAssets.SquareOutlineThinVertices, PaAssets.SquareOutlineThinIndices),
        ]);

        meshes.Add([
            renderingFactory.CreateMesh(PaAssets.CircleFilledVertices, PaAssets.CircleFilledIndices),
            renderingFactory.CreateMesh(PaAssets.CircleOutlineVertices, PaAssets.CircleOutlineIndices),
            renderingFactory.CreateMesh(PaAssets.CircleHalfVertices, PaAssets.CircleHalfIndices),
            renderingFactory.CreateMesh(PaAssets.CircleHalfOutlineVertices, PaAssets.CircleHalfOutlineIndices),
            renderingFactory.CreateMesh(PaAssets.CircleOutlineThinVertices, PaAssets.CircleOutlineThinIndices),
            renderingFactory.CreateMesh(PaAssets.CircleQuarterVertices, PaAssets.CircleQuarterIndices),
            renderingFactory.CreateMesh(PaAssets.CircleQuarterOutlineVertices, PaAssets.CircleQuarterOutlineIndices),
            renderingFactory.CreateMesh(PaAssets.CircleHalfQuarterVertices, PaAssets.CircleHalfQuarterIndices),
            renderingFactory.CreateMesh(PaAssets.CircleHalfQuarterOutlineVertices, PaAssets.CircleHalfQuarterOutlineIndices),
        ]);

        meshes.Add([
            renderingFactory.CreateMesh(PaAssets.TriangleFilledVertices, PaAssets.TriangleFilledIndices),
            renderingFactory.CreateMesh(PaAssets.TriangleOutlineVertices, PaAssets.TriangleOutlineIndices),
            renderingFactory.CreateMesh(PaAssets.TriangleRightFilledVertices, PaAssets.TriangleRightFilledIndices),
            renderingFactory.CreateMesh(PaAssets.TriangleRightOutlineVertices, PaAssets.TriangleRightOutlineIndices),
        ]);

        meshes.Add([
            renderingFactory.CreateMesh(PaAssets.ArrowVertices, PaAssets.ArrowIndices),
            renderingFactory.CreateMesh(PaAssets.ArrowHeadVertices, PaAssets.ArrowHeadIndices),
        ]);

        meshes.Add([]);

        meshes.Add([
            renderingFactory.CreateMesh(PaAssets.HexagonFilledVertices, PaAssets.HexagonFilledIndices),
            renderingFactory.CreateMesh(PaAssets.HexagonOutlineVertices, PaAssets.HexagonOutlineIndices),
            renderingFactory.CreateMesh(PaAssets.HexagonOutlineThinVertices, PaAssets.HexagonOutlineThinIndices),
            renderingFactory.CreateMesh(PaAssets.HexagonHalfVertices, PaAssets.HexagonHalfIndices),
            renderingFactory.CreateMesh(PaAssets.HexagonHalfOutlineVertices, PaAssets.HexagonHalfOutlineIndices),
            renderingFactory.CreateMesh(PaAssets.HexagonHalfOutlineThinVertices, PaAssets.HexagonHalfOutlineThinIndices),
        ]);
    }

    public bool TryGetMeshForShape(int shape, int shapeOption, [MaybeNullWhen(false)] out IMesh mesh)
    {
        if (shape < meshes.Count && shapeOption < meshes[shape].Count)
        {
            mesh = meshes[shape][shapeOption];
            return true;
        }

        mesh = null;
        return false;
    }
}