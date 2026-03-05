using System.Numerics;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Rendering.Data;

namespace ParallelAnimationSystem.Rendering;

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

public enum DrawType
{
    Mesh,
    Text
}

public struct DrawCommand
{
    public DrawType DrawType;
    public int DrawId;
    public int DrawIndex;
}

public class DrawList
{
    public CameraData CameraData { get; set; } = new(Vector2.Zero, 10.0f, 0.0f);
    public PostProcessingData PostProcessingData { get; set; }
    public ColorRgba ClearColor { get; set; } = new(0.0f, 0.0f, 0.0f, 1.0f);
    
    public Span<MeshDrawItem> MeshDrawItems => meshDrawItems.AsSpan(0, meshDrawItemCount);
    public Span<TextDrawItem> TextDrawItems => textDrawItems.AsSpan(0, textDrawItemCount);
    public Span<DrawCommand> DrawCommands => drawCommands.AsSpan(0, drawCommandCount);

    private MeshDrawItem[] meshDrawItems = new MeshDrawItem[1000];
    private TextDrawItem[] textDrawItems = new TextDrawItem[1000];
    private DrawCommand[] drawCommands = new DrawCommand[1000];
    
    private int meshDrawItemCount;
    private int textDrawItemCount;
    private int drawCommandCount;
    
    public void AddMesh(MeshHandle mesh, Matrix3x2 transform, ColorRgba color1, ColorRgba color2, RenderMode renderMode)
    {
        EnsureIndexExists(ref meshDrawItems, meshDrawItemCount);
        ref var drawItem = ref meshDrawItems[meshDrawItemCount];
        drawItem.MeshHandle = mesh;
        drawItem.Transform = transform;
        drawItem.Color1 = color1;
        drawItem.Color2 = color2;
        drawItem.RenderMode = renderMode;
        
        EnsureIndexExists(ref drawCommands, drawCommandCount);
        ref var drawCommand = ref drawCommands[drawCommandCount];
        drawCommand.DrawType = DrawType.Mesh;
        drawCommand.DrawId = meshDrawItemCount;
        drawCommand.DrawIndex = drawCommandCount;
        drawCommandCount++;
        
        meshDrawItemCount++;
    }
    
    public void AddText(TextHandle text, Matrix3x2 transform, ColorRgba color)
    {
        EnsureIndexExists(ref textDrawItems, textDrawItemCount);
        ref var drawItem = ref textDrawItems[textDrawItemCount];
        drawItem.TextHandle = text;
        drawItem.Transform = transform;
        drawItem.Color = color;
        
        EnsureIndexExists(ref drawCommands, drawCommandCount);
        ref var drawCommand = ref drawCommands[drawCommandCount];
        drawCommand.DrawType = DrawType.Text;
        drawCommand.DrawId = textDrawItemCount;
        drawCommand.DrawIndex = drawCommandCount;
        drawCommandCount++;
        
        textDrawItemCount++;
    }
    
    public void Reset()
    {
        CameraData = new CameraData(Vector2.Zero, 10.0f, 0.0f);
        PostProcessingData = default;
        ClearColor = new ColorRgba(0.0f, 0.0f, 0.0f, 1.0f);
        
        meshDrawItemCount = 0;
        textDrawItemCount = 0;
        drawCommandCount = 0;
    }

    private static void EnsureIndexExists<T>(ref T[] drawItems, int index) where T : struct
    {
        if (index < drawItems.Length)
            return;
        
        // add new item if index exceeds current count
        var newSize = Math.Max(drawItems.Length * 2, index + 1);
        Array.Resize(ref drawItems, newSize);
    }
}