using System.Collections.Concurrent;
using System.Numerics;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Text;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering.Data;
using ParallelAnimationSystem.Rendering.Handle;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Rendering;

public class AsyncRenderQueue : IRenderQueue
{
    private class Ref<T>(T value) where T : struct
    {
        public T Value { get; set; } = value;
    }

    private class DrawList(AsyncRenderQueue renderQueue) : IDrawList, IDrawDataProvider, IResettable
    {
        private struct RefMeshDrawItem
        {
            public Ref<MeshHandle> MeshHandle;
            public Matrix3x2 Transform;
            public ColorRgba Color1;
            public ColorRgba Color2;
            public RenderMode RenderMode;
            public float GradientRotation;
            public float GradientScale;
        }

        private struct RefTextDrawItem
        {
            public Ref<TextHandle> TextHandle;
            public Matrix3x2 Transform;
            public ColorRgba Color;
        }
        
        public CameraState CameraState { get; set; }
        public PostProcessingState PostProcessingState { get; set; }
        public ColorRgba ClearColor { get; set; }
        
        private RefMeshDrawItem[] meshDrawItems = new RefMeshDrawItem[1000];
        private RefTextDrawItem[] textDrawItems = new RefTextDrawItem[1000];
        private DrawCommand[] drawCommands = new DrawCommand[1000];
        
        private MeshDrawItem[] cachedMeshDrawItems = new MeshDrawItem[1000];
        private TextDrawItem[] cachedTextDrawItems = new TextDrawItem[1000];
        
        private int meshDrawItemCount;
        private int textDrawItemCount;
        private int drawCommandCount;
        
        public void AddMesh(MeshHandle mesh, Matrix3x2 transform, ColorRgba color1, ColorRgba color2, RenderMode renderMode, float gradientRotation, float gradientScale)
        {
            EnsureCount(ref meshDrawItems, meshDrawItemCount + 1);
            ref var drawItem = ref meshDrawItems[meshDrawItemCount];
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
            EnsureCount(ref textDrawItems, textDrawItemCount + 1);
            ref var drawItem = ref textDrawItems[textDrawItemCount];
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
        {
            EnsureCount(ref cachedMeshDrawItems, meshDrawItemCount);
            for (var i = 0; i < meshDrawItemCount; i++)
            {
                ref var refItem = ref meshDrawItems[i];
                ref var cachedItem = ref cachedMeshDrawItems[i];
                cachedItem.MeshHandle = refItem.MeshHandle.Value;
                cachedItem.Transform = refItem.Transform;
                cachedItem.Color1 = refItem.Color1;
                cachedItem.Color2 = refItem.Color2;
                cachedItem.RenderMode = refItem.RenderMode;
                cachedItem.GradientRotation = refItem.GradientRotation;
                cachedItem.GradientScale = refItem.GradientScale;
            }
            
            EnsureCount(ref cachedTextDrawItems, textDrawItemCount);
            for (var i = 0; i < textDrawItemCount; i++)
            {
                ref var refItem = ref textDrawItems[i];
                ref var cachedItem = ref cachedTextDrawItems[i];
                cachedItem.TextHandle = refItem.TextHandle.Value;
                cachedItem.Transform = refItem.Transform;
                cachedItem.Color = refItem.Color;
            }
            
            return new DrawData
            {
                CameraState = CameraState,
                PostProcessingState = PostProcessingState,
                ClearColor = ClearColor,
                MeshDrawItems = cachedMeshDrawItems.AsSpan(0, meshDrawItemCount),
                TextDrawItems = cachedTextDrawItems.AsSpan(0, textDrawItemCount),
                DrawCommands = drawCommands.AsSpan(0, drawCommandCount)
            };
        }
        
        private static void EnsureCount<T>(ref T[] drawItems, int count) where T : struct
        {
            if (count <= drawItems.Length)
                return;
        
            // add new item if index exceeds current count
            var newSize = Math.Max(drawItems.Length * 2, count);
            Array.Resize(ref drawItems, newSize);
        }
    }

    private const int RenderAheadLimit = 3;

    public int QueuedFrames => drawListPool.RentedCount;

    private readonly MemoryPool<DrawList> drawListPool;
    
    private readonly ConcurrentQueue<(Action<IRenderer> Action, bool IsFrame)> renderThreadActions = new();
    
    private readonly SparseSet<Ref<MeshHandle>> meshes = new();
    private readonly SparseSet<Ref<FontHandle>> fonts = new();
    private readonly SparseSet<Ref<TextHandle>> texts = new();

    private readonly IRenderingFactory renderingFactory;
    
    public AsyncRenderQueue(IRenderingFactory renderingFactory)
    {
        this.renderingFactory = renderingFactory;
        
        drawListPool = new MemoryPool<DrawList>(RenderAheadLimit, () => new DrawList(this));
    }
    
    public MeshHandle CreateMesh(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<int> indices)
    {
        // copy data
        var verticesCopy = new Vector2[vertices.Length];
        vertices.CopyTo(verticesCopy);
        
        var indicesCopy = new int[indices.Length];
        indices.CopyTo(indicesCopy);
        
        // create new ref
        var meshRef = new Ref<MeshHandle>(new MeshHandle(-1));
        
        // add ref to collection to keep track of it
        var id = meshes.Insert(meshRef);
        
        // queue action to create mesh on render thread
        renderThreadActions.Enqueue((_ =>
        {
            // create mesh on render thread
            meshRef.Value = renderingFactory.CreateMesh(verticesCopy, indicesCopy);
        }, false));
        
        return new MeshHandle(id);
    }

    public void DestroyMesh(MeshHandle handle)
    {
        if (!meshes.Remove(handle.Id, out var meshRef))
            throw new ArgumentException($"Invalid mesh handle '{handle.Id}'", nameof(handle));
        
        // queue action to destroy mesh on render thread
        renderThreadActions.Enqueue((_ =>
        {            
            renderingFactory.DestroyMesh(meshRef.Value);
            meshRef.Value = new MeshHandle(-1);
        }, false));
    }

    public FontHandle CreateFont(int width, int height, ReadOnlySpan<byte> atlas)
    {
        // copy data
        var atlasCopy = new byte[atlas.Length];
        atlas.CopyTo(atlasCopy);
        
        // create new ref
        var fontRef = new Ref<FontHandle>(new FontHandle(-1));
        
        // add ref to collection to keep track of it
        var id = fonts.Insert(fontRef);
        
        // queue action to create font on render thread
        renderThreadActions.Enqueue((_ =>
        {
            // create font on render thread
            fontRef.Value = renderingFactory.CreateFont(width, height, atlasCopy);
        }, false));
        
        return new FontHandle(id);
    }

    public void DestroyFont(FontHandle handle)
    {
        if (!fonts.Remove(handle.Id, out var fontRef))
            throw new ArgumentException($"Invalid font handle '{handle.Id}'", nameof(handle));
        
        // queue action to destroy font on render thread
        renderThreadActions.Enqueue((_ =>
        {            
            renderingFactory.DestroyFont(fontRef.Value);
            fontRef.Value = new FontHandle(-1);
        }, false));
    }

    public TextHandle CreateText(ShapedRichText richText)
    {
        // copy data
        var shapedRichTextCopy = CopyShapedRichText(richText);
        
        // create new ref
        var textRef = new Ref<TextHandle>(new TextHandle(-1));
        
        // add ref to collection to keep track of it
        var id = texts.Insert(textRef);
        
        // queue action to create text on render thread
        renderThreadActions.Enqueue((_ =>
        {
            // create text on render thread
            textRef.Value = renderingFactory.CreateText(shapedRichTextCopy);
        }, false));
        
        return new TextHandle(id);
    }

    public void DestroyText(TextHandle handle)
    {
        if (!texts.Remove(handle.Id, out var textRef))
            throw new ArgumentException($"Invalid text handle '{handle.Id}'", nameof(handle));
        
        // queue action to destroy text on render thread
        renderThreadActions.Enqueue((_ =>
        {            
            renderingFactory.DestroyText(textRef.Value);
            textRef.Value = new TextHandle(-1);
        }, false));
    }

    public IDrawList GetDrawList()
    {
        DrawList drawList;
        while (!drawListPool.TryRent(out drawList!))
            Thread.Yield(); // wait until a draw list is available

        return drawList;
    }

    public void SubmitDrawList(IDrawList drawList)
    {
        var actualDrawList = (DrawList)drawList;
        
        renderThreadActions.Enqueue((renderer =>
        {
            // submit draw list to renderer
            renderer.ProcessFrame(actualDrawList);
            
            // return draw list to pool
            drawListPool.Return(actualDrawList);
        }, true));
    }

    public void FlushOneFrame(IRenderer renderer)
    {
        while (renderThreadActions.TryDequeue(out var action))
        {
            action.Action(renderer);
            if (action.IsFrame)
                break;
        }
    }
    
    public void Flush(IRenderer renderer)
    {
        while (renderThreadActions.TryDequeue(out var action))
            action.Action(renderer);
    }

    private static ShapedRichText CopyShapedRichText(ShapedRichText shapedRichText)
    {
        var newShapedRichText = new ShapedRichText();
        newShapedRichText.Glyphs.EnsureCapacity(shapedRichText.Glyphs.Count);
        newShapedRichText.Glyphs.AddRange(shapedRichText.Glyphs.Select(CopyShapedTextGlyph));
        return newShapedRichText;
    }
    
    private static ShapedTextGlyph CopyShapedTextGlyph(ShapedTextGlyph glyph)
        => new(glyph.Min, glyph.Max, glyph.MinUV, glyph.MaxUV, glyph.Color, glyph.BoldItalic, glyph.Font);
}