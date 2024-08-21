using OpenTK.Mathematics;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem.Data;

public record struct DrawData(MeshHandle Mesh, Matrix3 Transform, float Z, Color4 Color);