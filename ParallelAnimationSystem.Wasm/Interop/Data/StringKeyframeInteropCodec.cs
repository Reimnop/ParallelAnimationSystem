using System.Runtime.CompilerServices;
using System.Text;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Wasm.Interop.Data;

public class StringKeyframeInteropCodec : IDynamicSizeKeyframeInteropCodec<Keyframe<string>>
{
    public int GetSize(Keyframe<string> keyframe)
    {
        var stringByteCount = Encoding.UTF8.GetByteCount(keyframe.Value);
        // time (4 bytes) + ease (4 bytes) + string data (stringByteCount) + null terminator (1 byte)
        return 4 + 4 + stringByteCount + 1;
    }

    public unsafe int Write(Keyframe<string> keyframe, IntPtr bufferPtr)
    {
        var ptr = (byte*)bufferPtr.ToPointer();
        
        Unsafe.Write(ptr, keyframe.Time);
        Unsafe.Write(ptr + 4, keyframe.Ease);
        
        var stringByteCount = Encoding.UTF8.GetByteCount(keyframe.Value);
        var stringSpan = new Span<byte>(ptr + 8, stringByteCount + 1); // +1 for null terminator
        Encoding.UTF8.GetBytes(keyframe.Value, stringSpan);
        stringSpan[stringByteCount] = 0; // null terminator
        
        return 4 + 4 + stringByteCount + 1;
    }

    public unsafe Keyframe<string> Read(IntPtr bufferPtr, out int bytesRead)
    {
        var ptr = (byte*)bufferPtr.ToPointer();
        
        var time = Unsafe.Read<float>(ptr);
        var ease = Unsafe.Read<Ease>(ptr + 4);
        
        // find null terminator to determine string length
        var stringStart = ptr + 8;
        var stringByteCount = 0;
        while (stringStart[stringByteCount] != 0)
            stringByteCount++;
        
        var value = Encoding.UTF8.GetString(stringStart, stringByteCount);
        
        bytesRead = 4 + 4 + stringByteCount + 1;
        return new Keyframe<string>(time, ease, value);
    }
}