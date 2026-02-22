using System.Numerics;
using System.Runtime.InteropServices;
using ParallelAnimationSystem.Core.Model;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropBeatmapPrefabInstance
{
    [UnmanagedCallersOnly(EntryPoint = "beatmapPrefabInstance_new")]
    public static IntPtr New(IntPtr idPtr)
    {
        var id = Marshal.PtrToStringUTF8(idPtr);
        if (id is null)
            return IntPtr.Zero;
        var prefabInstance = new BeatmapPrefabInstance(id);
        return InteropHelper.ObjectToIntPtr(prefabInstance);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapPrefabInstance_getId")]
    public static IntPtr GetId(IntPtr ptr)
    {
        var prefabInstance = InteropHelper.IntPtrToObject<BeatmapPrefabInstance>(ptr);
        return InteropHelper.ObjectToIntPtr(prefabInstance.Id);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapPrefabInstance_getStartTime")]
    public static float GetStartTime(IntPtr ptr)
    {
        var prefabInstance = InteropHelper.IntPtrToObject<BeatmapPrefabInstance>(ptr);
        return prefabInstance.StartTime;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapPrefabInstance_setStartTime")]
    public static void SetStartTime(IntPtr ptr, float value)
    {
        var prefabInstance = InteropHelper.IntPtrToObject<BeatmapPrefabInstance>(ptr);
        prefabInstance.StartTime = value;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapPrefabInstance_getPosition")]
    public static unsafe void GetPosition(IntPtr ptr, float* positionPtr)
    {
        var prefabInstance = InteropHelper.IntPtrToObject<BeatmapPrefabInstance>(ptr);
        positionPtr[0] = prefabInstance.Position.X;
        positionPtr[1] = prefabInstance.Position.Y;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapPrefabInstance_setPosition")]
    public static unsafe void SetPosition(IntPtr ptr, float* positionPtr)
    {
        var prefabInstance = InteropHelper.IntPtrToObject<BeatmapPrefabInstance>(ptr);
        prefabInstance.Position = new Vector2(positionPtr[0], positionPtr[1]);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "beatmapPrefabInstance_getScale")]
    public static unsafe void GetScale(IntPtr ptr, float* scalePtr)
    {
        var prefabInstance = InteropHelper.IntPtrToObject<BeatmapPrefabInstance>(ptr);
        scalePtr[0] = prefabInstance.Scale.X;
        scalePtr[1] = prefabInstance.Scale.Y;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapPrefabInstance_setScale")]
    public static unsafe void SetScale(IntPtr ptr, float* scalePtr)
    {
        var prefabInstance = InteropHelper.IntPtrToObject<BeatmapPrefabInstance>(ptr);
        prefabInstance.Scale = new Vector2(scalePtr[0], scalePtr[1]);
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapPrefabInstance_getRotation")]
    public static float GetRotation(IntPtr ptr)
    {
        var prefabInstance = InteropHelper.IntPtrToObject<BeatmapPrefabInstance>(ptr);
        return prefabInstance.Rotation;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapPrefabInstance_setRotation")]
    public static void SetRotation(IntPtr ptr, float value)
    {
        var prefabInstance = InteropHelper.IntPtrToObject<BeatmapPrefabInstance>(ptr);
        prefabInstance.Rotation = value;
    }

    [UnmanagedCallersOnly(EntryPoint = "beatmapPrefabInstance_getPrefabId")]
    public static IntPtr GetPrefabId(IntPtr ptr)
    {
        var prefabInstance = InteropHelper.IntPtrToObject<BeatmapPrefabInstance>(ptr);
        if (prefabInstance.PrefabId is null)
            return IntPtr.Zero;
        return InteropHelper.ObjectToIntPtr(prefabInstance.PrefabId);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "beatmapPrefabInstance_setPrefabId")]
    public static void SetPrefabId(IntPtr ptr, IntPtr prefabIdPtr)
    {        
        var prefabInstance = InteropHelper.IntPtrToObject<BeatmapPrefabInstance>(ptr);
        if (prefabIdPtr == IntPtr.Zero)
        {
            prefabInstance.PrefabId = null;
            return;
        }
        
        var prefabId = Marshal.PtrToStringUTF8(prefabIdPtr);
        prefabInstance.PrefabId = prefabId;
    }
}