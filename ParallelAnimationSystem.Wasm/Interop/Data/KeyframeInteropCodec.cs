using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Wasm.Interop.Data;

public class KeyframeInteropCodec<T> : IKeyframeInteropCodec<Keyframe<T>> where T : unmanaged
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Buffer
    {
        public float Time;
        public Ease Ease;
        public T Value;
    }

    public int Size => Unsafe.SizeOf<Buffer>();

    public unsafe void Write(Keyframe<T> keyframe, IntPtr bufferPtr)
    {
        var data = new Buffer
        {
            Time = keyframe.Time,
            Ease = keyframe.Ease,
            Value = keyframe.Value
        };
        Unsafe.Write(bufferPtr.ToPointer(), data);
    }

    public unsafe Keyframe<T> Read(IntPtr bufferPtr)
    {
        var buffer = Unsafe.Read<Buffer>(bufferPtr.ToPointer());
        return new Keyframe<T>(buffer.Time, buffer.Ease, buffer.Value);
    }
}