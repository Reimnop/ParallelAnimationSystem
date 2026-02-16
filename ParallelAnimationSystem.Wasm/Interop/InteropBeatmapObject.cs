using System.Numerics;
using System.Runtime.InteropServices;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Model;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropBeatmapObject
{
    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_new")]
    public static IntPtr New(IntPtr idPtr)
    {
        var id = Marshal.PtrToStringUTF8(idPtr);
        if (id is null)
            return IntPtr.Zero;
        var beatmapObject = new BeatmapObject(id);
        return InteropHelper.ObjectToIntPtr(beatmapObject);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_getId")]
    public static IntPtr GetId(IntPtr ptr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        var id = beatmapObject.Id;
        return InteropHelper.StringToIntPtrUTF8(id);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_getName")]
    public static IntPtr GetName(IntPtr ptr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        var name = beatmapObject.Name;
        return InteropHelper.StringToIntPtrUTF8(name);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_setName")]
    public static void SetName(IntPtr ptr, IntPtr namePtr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        var name = Marshal.PtrToStringUTF8(namePtr);
        if (name is null)
            return;
        beatmapObject.Name = name;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_getParentId")]
    public static IntPtr GetParentId(IntPtr ptr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        var parentId = beatmapObject.ParentId;
        if (parentId is null)
            return IntPtr.Zero;
        return InteropHelper.StringToIntPtrUTF8(parentId);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_setParentId")]
    public static void SetParentId(IntPtr ptr, IntPtr parentIdPtr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        if (parentIdPtr == IntPtr.Zero)
        {
            beatmapObject.ParentId = null;
            return;
        }

        var parentId = Marshal.PtrToStringUTF8(parentIdPtr);
        beatmapObject.ParentId = parentId;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_getType")]
    public static int GetType(IntPtr ptr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        return (int)beatmapObject.Type;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_setType")]
    public static void SetType(IntPtr ptr, int type)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        beatmapObject.Type = (BeatmapObjectType)type;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_getParentType")]
    public static int GetParentType(IntPtr ptr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        return (int)beatmapObject.ParentType;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_setParentType")]
    public static void SetParentType(IntPtr ptr, int parentType)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        beatmapObject.ParentType = (ParentType)parentType;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_getParentOffset")]
    public static unsafe void GetParentOffset(IntPtr ptr, float* parentOffsetPtr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        var offset = beatmapObject.ParentOffset;
        parentOffsetPtr[0] = offset.Position;
        parentOffsetPtr[1] = offset.Scale;
        parentOffsetPtr[2] = offset.Rotation;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_setParentOffset")]
    public static unsafe void SetParentOffset(IntPtr ptr, float* parentOffsetPtr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        var offset = new ParentOffset
        {
            Position = parentOffsetPtr[0],
            Scale = parentOffsetPtr[1],
            Rotation = parentOffsetPtr[2]
        };
        beatmapObject.ParentOffset = offset;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_getRenderType")]
    public static int GetRenderType(IntPtr ptr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        return (int)beatmapObject.RenderType;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_setRenderType")]
    public static void SetRenderType(IntPtr ptr, int renderType)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        beatmapObject.RenderType = (RenderType)renderType;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_getOrigin")]
    public static unsafe void GetOrigin(IntPtr ptr, float* originPtr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        var origin = beatmapObject.Origin;
        originPtr[0] = origin.X;
        originPtr[1] = origin.Y;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_setOrigin")]
    public static unsafe void SetOrigin(IntPtr ptr, float* originPtr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        var origin = new Vector2(originPtr[0], originPtr[1]);
        beatmapObject.Origin = origin;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_getRenderDepth")]
    public static float GetRenderDepth(IntPtr ptr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        return beatmapObject.RenderDepth;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_setRenderDepth")]
    public static void SetRenderDepth(IntPtr ptr, float renderDepth)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        beatmapObject.RenderDepth = renderDepth;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_getStartTime")]
    public static float GetStartTime(IntPtr ptr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        return beatmapObject.StartTime;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_setStartTime")]
    public static void SetStartTime(IntPtr ptr, float startTime)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        beatmapObject.StartTime = startTime;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_getAutoKillType")]
    public static int GetAutoKillType(IntPtr ptr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        return (int)beatmapObject.AutoKillType;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_setAutoKillType")]
    public static void SetAutoKillType(IntPtr ptr, int autoKillType)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        beatmapObject.AutoKillType = (AutoKillType)autoKillType;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_getAutoKillOffset")]
    public static float GetAutoKillOffset(IntPtr ptr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        return beatmapObject.AutoKillOffset;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_setAutoKillOffset")]
    public static void SetAutoKillOffset(IntPtr ptr, float autoKillOffset)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        beatmapObject.AutoKillOffset = autoKillOffset;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_getShape")]
    public static int GetShape(IntPtr ptr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        return (int)beatmapObject.Shape;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_setShape")]
    public static void SetShape(IntPtr ptr, int shape)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        beatmapObject.Shape = (ObjectShape)shape;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_getText")]
    public static IntPtr GetText(IntPtr ptr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        var text = beatmapObject.Text;
        if (text is null)
            return IntPtr.Zero;
        return InteropHelper.StringToIntPtrUTF8(text);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapObject_setText")]
    public static void SetText(IntPtr ptr, IntPtr textPtr)
    {
        var beatmapObject = InteropHelper.IntPtrToObject<BeatmapObject>(ptr);
        if (textPtr == IntPtr.Zero)
        {
            beatmapObject.Text = null;
            return;
        }

        var text = Marshal.PtrToStringUTF8(textPtr);
        beatmapObject.Text = text;
    }
}