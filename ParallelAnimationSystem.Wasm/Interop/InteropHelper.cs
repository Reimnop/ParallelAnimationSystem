using System.Runtime.InteropServices;
using System.Text;

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
    
    public static unsafe IntPtr StringToIntPtrUTF8(string str)
    {
        var utf8ByteCount = Encoding.UTF8.GetByteCount(str);
        var buffer = NativeMemory.Alloc((UIntPtr)utf8ByteCount + 1);
        try
        {
            var bufferSpan = new Span<byte>(buffer, utf8ByteCount + 1);
            Encoding.UTF8.GetBytes(str, bufferSpan);
            bufferSpan[utf8ByteCount] = 0; // null terminator
            return (IntPtr)buffer;
        }
        catch
        {
            NativeMemory.Free(buffer);
            throw;
        }
    }
    
    [UnmanagedCallersOnly(EntryPoint = "interop_releasePointer")]
    public static void ReleasePointer(IntPtr ptr)
    {
        var handle = GCHandle.FromIntPtr(ptr);
        handle.Free();
    }
    
    [UnmanagedCallersOnly(EntryPoint = "interop_alloc")]
    public static unsafe void* Alloc(UIntPtr size)
    {        
        return NativeMemory.Alloc(size);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "interop_free")]
    public static unsafe void Free(void* ptr)
    {
        NativeMemory.Free(ptr);
    }
}