using System.Collections.Concurrent;
using System.Numerics;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Text;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering.Data;
using ParallelAnimationSystem.Rendering.Handle;
using ParallelAnimationSystem.Util;
using Tmpx.Common;

namespace ParallelAnimationSystem.Rendering;

public class RenderQueue
{
    private delegate void RenderThreadAction(IRenderingFactory renderingFactory, IRenderer renderer);
    
    private class ValueRef<T>(T value) where T : struct
    {
        public T Value { get; set; } = value;
    }
    
    private class DrawList(RenderQueue renderQueue) : IDrawList, IDrawDataProvider, IResettable
    {
        private struct RefMeshDrawItem
        {
            public ValueRef<MeshHandle> MeshHandle;
            public Matrix3x2 Transform;
            public ColorRgba Color1;
            public ColorRgba Color2;
            public RenderMode RenderMode;
            public float GradientRotation;
            public float GradientScale;
        }

        private struct RefTextDrawItem
        {
            public ValueRef<TextHandle> TextHandle;
            public Matrix3x2 Transform;
            public ColorRgba Color;
        }

        public CameraState CameraState { get; set; }
        public PostProcessingState PostProcessingState { get; set; }
        public ColorRgba ClearColor { get; set; }

        private RefMeshDrawItem[] refMeshDrawItems = new RefMeshDrawItem[1000];
        private RefTextDrawItem[] refTextDrawItems = new RefTextDrawItem[1000];
        private DrawCommand[] drawCommands = new DrawCommand[1000];

        private MeshDrawItem[] materializedMeshDrawItems = new MeshDrawItem[1000];
        private TextDrawItem[] materializedTextDrawItems = new TextDrawItem[1000];

        private int meshDrawItemCount;
        private int textDrawItemCount;
        private int drawCommandCount;

        public void AddMesh(MeshHandle mesh, Matrix3x2 transform, ColorRgba color1, ColorRgba color2, RenderMode renderMode, float gradientRotation, float gradientScale)
        {
            EnsureCount(ref refMeshDrawItems, meshDrawItemCount + 1);
            ref var drawItem = ref refMeshDrawItems[meshDrawItemCount];
            drawItem.MeshHandle = renderQueue.meshes[mesh.Id];
            drawItem.Transform = transform;
            drawItem.Color1 = color1;
            drawItem.Color2 = color2;
            drawItem.RenderMode = renderMode;
            drawItem.GradientRotation = gradientRotation;
            drawItem.GradientScale = gradientScale;

            EnsureCount(ref drawCommands, drawCommandCount + 1);
            ref var drawCommand = ref drawCommands[drawCommandCount];
            drawCommand.DrawType = DrawType.Mesh;
            drawCommand.DrawId = meshDrawItemCount;
            drawCommandCount++;
            meshDrawItemCount++;
        }

        public void AddText(TextHandle text, Matrix3x2 transform, ColorRgba color)
        {
            EnsureCount(ref refTextDrawItems, textDrawItemCount + 1);
            ref var drawItem = ref refTextDrawItems[textDrawItemCount];
            drawItem.TextHandle = renderQueue.texts[text.Id];
            drawItem.Transform = transform;
            drawItem.Color = color;

            EnsureCount(ref drawCommands, drawCommandCount + 1);
            ref var drawCommand = ref drawCommands[drawCommandCount];
            drawCommand.DrawType = DrawType.Text;
            drawCommand.DrawId = textDrawItemCount;
            drawCommandCount++;
            textDrawItemCount++;
        }

        public void Reset()
        {
            CameraState = new CameraState { Scale = 10f };
            PostProcessingState = default;
            ClearColor = new ColorRgba(0.0f, 0.0f, 0.0f, 1.0f);
            meshDrawItemCount = 0;
            textDrawItemCount = 0;
            drawCommandCount = 0;
        }

        public DrawData CreateDrawData()
        {
            EnsureCount(ref materializedMeshDrawItems, meshDrawItemCount);
            for (var i = 0; i < meshDrawItemCount; i++)
            {
                ref var refItem = ref refMeshDrawItems[i];
                ref var item = ref materializedMeshDrawItems[i];
                item.MeshHandle = refItem.MeshHandle.Value;
                item.Transform = refItem.Transform;
                item.Color1 = refItem.Color1;
                item.Color2 = refItem.Color2;
                item.RenderMode = refItem.RenderMode;
                item.GradientRotation = refItem.GradientRotation;
                item.GradientScale = refItem.GradientScale;
            }

            EnsureCount(ref materializedTextDrawItems, textDrawItemCount);
            for (var i = 0; i < textDrawItemCount; i++)
            {
                ref var refItem = ref refTextDrawItems[i];
                ref var item = ref materializedTextDrawItems[i];
                item.TextHandle = refItem.TextHandle.Value;
                item.Transform = refItem.Transform;
                item.Color = refItem.Color;
            }

            return new DrawData
            {
                CameraState = CameraState,
                PostProcessingState = PostProcessingState,
                ClearColor = ClearColor,
                MeshDrawItems = materializedMeshDrawItems.AsSpan(0, meshDrawItemCount),
                TextDrawItems = materializedTextDrawItems.AsSpan(0, textDrawItemCount),
                DrawCommands = drawCommands.AsSpan(0, drawCommandCount)
            };
        }

        private static void EnsureCount<T>(ref T[] arr, int count) where T : struct
        {
            if (count <= arr.Length) return;
            Array.Resize(ref arr, Math.Max(arr.Length * 2, count));
        }
    }

    private class FrameCommandList(RenderQueue renderQueue) : IResettable
    {
        public DrawList DrawList { get; } = new(renderQueue);
        
        // Only enqueued on the director thread
        private readonly Queue<RenderThreadAction> renderThreadActions = [];
        
        public void Push(RenderThreadAction action)
        {
            renderThreadActions.Enqueue(action);
        }
        
        public void Flush(IRenderingFactory renderingFactory, IRenderer renderer)
        {
            while (renderThreadActions.Count > 0)
            {
                var action = renderThreadActions.Dequeue();
                action(renderingFactory, renderer);
            }
        }

        public void Reset()
        {
            renderThreadActions.Clear();
        }
    }

    private const int RenderAheadLimit = 3;

    public int FreeFrameCount => frameCommandListPool.FreeCount;
    public int QueuedFrameCount => queuedFrames.Count;

    private readonly SparseSet<ValueRef<MeshHandle>> meshes = new();
    private readonly SparseSet<ValueRef<TextHandle>> texts = new();
    
    private readonly ConcurrentMemoryPool<FrameCommandList> frameCommandListPool;
    private readonly ConcurrentQueue<FrameCommandList> queuedFrames = [];

    private readonly IRenderingFactory renderingFactory;

    private FrameCommandList? currentFrame;
    
    public RenderQueue(IRenderingFactory renderingFactory)
    {
        this.renderingFactory = renderingFactory;
        frameCommandListPool = new ConcurrentMemoryPool<FrameCommandList>(RenderAheadLimit, () => new FrameCommandList(this));
    }

    public MeshHandle CreateMesh(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<int> indices)
    {
        var verticesCopy = vertices.ToArray();
        var indicesCopy = indices.ToArray();
        var meshRef = new ValueRef<MeshHandle>(new MeshHandle(-1));
        var id = meshes.Insert(meshRef);
        var frame = GetCurrentFrameCommandList();
        frame.Push((factory, _) => meshRef.Value = factory.CreateMesh(verticesCopy, indicesCopy));
        return new MeshHandle(id);
    }

    public void DestroyMesh(MeshHandle handle)
    {
        if (!meshes.Remove(handle.Id, out var meshRef))
            throw new ArgumentException($"Invalid mesh handle '{handle.Id}'", nameof(handle));
        var frame = GetCurrentFrameCommandList();
        frame.Push((factory, _) =>
        {
            factory.DestroyMesh(meshRef.Value);
            meshRef.Value = new MeshHandle(-1);
        });
    }

    public void SetFontBuffers(
        ReadOnlySpan<QuadraticCurve> curves,
        ReadOnlySpan<int> curveIndices,
        ReadOnlySpan<BandEntry> bandEntries,
        ReadOnlySpan<ShapeEntry> shapeEntries)
    {
        var curvesCopy = curves.ToArray();
        var curveIndicesCopy = curveIndices.ToArray();
        var bandEntriesCopy = bandEntries.ToArray();
        var shapeEntriesCopy = shapeEntries.ToArray();
        var frame = GetCurrentFrameCommandList();
        frame.Push((factory, _) => factory.SetFontBuffers(curvesCopy, curveIndicesCopy, bandEntriesCopy, shapeEntriesCopy));
    }

    public TextHandle CreateText(ShapedRichText richText)
    {
        // ShapedRichText is already value-complete (global indices, no font references) so we can
        // pass it directly without any ref-tracking indirection.
        var richTextCopy = new ShapedRichText
        {
            Glyphs = richText.Glyphs
                .Select(g => new ShapedTextGlyph(g.Transform, g.Color, g.ShapeEntryIndex))
                .ToList()
        };
        var textRef = new ValueRef<TextHandle>(new TextHandle(-1));
        var id = texts.Insert(textRef);
        var frame = GetCurrentFrameCommandList();
        frame.Push((factory, _) => textRef.Value = factory.CreateText(richTextCopy));
        return new TextHandle(id);
    }

    public void DestroyText(TextHandle handle)
    {
        if (!texts.Remove(handle.Id, out var textRef))
            throw new ArgumentException($"Invalid text handle '{handle.Id}'", nameof(handle));
        var frame = GetCurrentFrameCommandList();
        frame.Push((factory, _) =>
        {
            factory.DestroyText(textRef.Value);
            textRef.Value = new TextHandle(-1);
        });
    }

    public IDrawList GetCurrentFrameDrawList()
    {
        var frame = GetCurrentFrameCommandList();
        return frame.DrawList;
    }
    
    public void FinishFrame()
    {
        var frame = GetCurrentFrameCommandList();
        var drawList = frame.DrawList;
        frame.Push((_, renderer) =>
        {
            renderer.ProcessFrame(drawList);
            drawList.Reset();
        });
        queuedFrames.Enqueue(frame);
        currentFrame = null;
    }

    /// <summary>
    /// Called on the render thread, takes the top frame and flushes it.
    /// </summary>
    public bool FlushFrame(IRenderer renderer)
    {
        if (!queuedFrames.TryDequeue(out var frame))
            return false;
        
        frame.Flush(renderingFactory, renderer);
        frameCommandListPool.Return(frame);
        return true;
    }

    private FrameCommandList GetCurrentFrameCommandList()
    {
        if (currentFrame == null)
        {
            // rent a new one
            if (!frameCommandListPool.TryRent(out currentFrame))
                // fallback to creating a new one if the pool is exhausted
                currentFrame = new FrameCommandList(this); 
        }
        
        return currentFrame;
    }
}
