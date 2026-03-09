using System.Numerics;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Rendering.Data;
using ParallelAnimationSystem.Rendering.Handle;

namespace ParallelAnimationSystem.Rendering;

public enum DrawType
{
    Mesh,
    Text
}

public struct DrawCommand
{
    public DrawType DrawType;
    public int DrawId;
}

public struct MeshDrawItem
{
    public MeshHandle MeshHandle;
    public Matrix3x2 Transform;
    public ColorRgba Color1;
    public ColorRgba Color2;
    public RenderMode RenderMode;
}

public struct TextDrawItem
{
    public TextHandle TextHandle;
    public Matrix3x2 Transform;
    public ColorRgba Color;
}

public ref struct DrawData
{
    public CameraData CameraData;
    public PostProcessingData PostProcessingData;
    public ColorRgba ClearColor;
    public Span<DrawCommand> DrawCommands;
    public Span<MeshDrawItem> MeshDrawItems;
    public Span<TextDrawItem> TextDrawItems;
}

public interface IDrawDataProvider
{
    DrawData CreateDrawData();
}