using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Wasm.Interop.Data;

// codec for fixed-size keyframe value types
public interface IFixedSizeKeyframeInteropCodec<T> where T : IKeyframe
{
    int Size { get; }
    void Write(T keyframe, IntPtr bufferPtr);
    T Read(IntPtr bufferPtr);
}