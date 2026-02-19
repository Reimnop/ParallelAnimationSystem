using System.Runtime.InteropServices;
using System.Text;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropString
{
    [UnmanagedCallersOnly(EntryPoint = "string_getLength")]
    public static int GetLength(IntPtr ptr)
    {
        var str = InteropHelper.IntPtrToObject<string>(ptr);
        return str.Length;
    }
    
    [UnmanagedCallersOnly(EntryPoint = "string_getByteCount")]
    public static int GetByteCount(IntPtr ptr)
    {
        var str = InteropHelper.IntPtrToObject<string>(ptr);
        return Encoding.UTF8.GetByteCount(str);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "string_getBytes")]
    public static unsafe void GetBytes(IntPtr ptr, IntPtr bufferPtr, int bufferSize)
    {
        var str = InteropHelper.IntPtrToObject<string>(ptr);
        var buffer = new Span<byte>(bufferPtr.ToPointer(), bufferSize);
        Encoding.UTF8.GetBytes(str, buffer);
    }
}