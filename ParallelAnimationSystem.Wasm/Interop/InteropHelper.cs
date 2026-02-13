using System.Runtime.InteropServices;

namespace ParallelAnimationSystem.Wasm.Interop;

public class InteropHelper
{
    public static IntPtr ObjectToIntPtr(object obj)
    {
        var handle = GCHandle.Alloc(obj);
        return GCHandle.ToIntPtr(handle);
    }
    
    public static T IntPtrToObject<T>(IntPtr ptr)
    {
        var handle = GCHandle.FromIntPtr(ptr);
        var target = handle.Target;
        if (target is not T typedTarget)
            throw new InvalidCastException($"Pointer does not point to an object of type {typeof(T).FullName}");
        return typedTarget;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "interop_releasePointer")]
    public static void ReleasePointer(IntPtr ptr)
    {
        var handle = GCHandle.FromIntPtr(ptr);
        handle.Free();
    }
}