using OpenTK.Mathematics;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem.Data;

public record struct DrawData(MeshHandle Mesh, Matrix3 Transform, float Z, RenderMode RenderMode, Color4 Color1, Color4 Color2);