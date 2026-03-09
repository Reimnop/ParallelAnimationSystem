using System.Numerics;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Text;
using ParallelAnimationSystem.Rendering.Data;
using ParallelAnimationSystem.Rendering.Handle;

namespace ParallelAnimationSystem.Rendering;

public class RenderQueue(IRenderingFactory renderingFactory) : IRenderQueue
{
    private class DrawList : IDrawList, IDrawDataProvider
    {
        public CameraState CameraState { get; set; }
        public PostProcessingState PostProcessingState { get; set; }
        public ColorRgba ClearColor { get; set; }

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
            drawCommandCount++;

            textDrawItemCount++;
        }

        public void Reset()
        {
            CameraState = new CameraState
            {
                Scale = 10f
            };
            PostProcessingState = default;
            ClearColor = new ColorRgba(0.0f, 0.0f, 0.0f, 1.0f);

            meshDrawItemCount = 0;
            textDrawItemCount = 0;
            drawCommandCount = 0;
        }

        public DrawData CreateDrawData()
            => new()
            {
                CameraState = CameraState,
                PostProcessingState = PostProcessingState,
                ClearColor = ClearColor,
                MeshDrawItems = meshDrawItems.AsSpan(0, meshDrawItemCount),
                TextDrawItems = textDrawItems.AsSpan(0, textDrawItemCount),
                DrawCommands = drawCommands.AsSpan(0, drawCommandCount)
            };

        private static void EnsureIndexExists<T>(ref T[] drawItems, int index) where T : struct
        {
            if (index < drawItems.Length)
                return;

            // add new item if index exceeds current count
            var newSize = Math.Max(drawItems.Length * 2, index + 1);
            Array.Resize(ref drawItems, newSize);
        }
    }
    
    private readonly DrawList drawList = new();

    public MeshHandle CreateMesh(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<int> indices)
    {
        return renderingFactory.CreateMesh(vertices, indices);
    }

    public void DestroyMesh(MeshHandle handle)
    {
        renderingFactory.DestroyMesh(handle);
    }

    public FontHandle CreateFont(int width, int height, ReadOnlySpan<byte> atlas)
    {
        return renderingFactory.CreateFont(width, height, atlas);
    }

    public void DestroyFont(FontHandle handle)
    {
        renderingFactory.DestroyFont(handle);
    }

    public TextHandle CreateText(ShapedRichText richText)
    {
        return renderingFactory.CreateText(richText);
    }

    public void DestroyText(TextHandle handle)
    {
        renderingFactory.DestroyText(handle);
    }

    public IDrawList GetDrawList()
    {
        drawList.Reset();
        return drawList;
    }

    public void SubmitDrawList(IDrawList drawList)
    {
        // do nothing
    }

    public void ProcessFrame(IRenderer renderer)
    {
        renderer.ProcessFrame(drawList);
    }
}