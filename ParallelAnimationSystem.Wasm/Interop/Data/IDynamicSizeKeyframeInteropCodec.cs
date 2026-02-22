using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Wasm.Interop.Data;

// codec for dynamically sized keyframe value types
public interface IDynamicSizeKeyframeInteropCodec<T> where T : IKeyframe
{
    int GetSize(T keyframe);
    int Write(T keyframe, IntPtr bufferPtr);
    T Read(IntPtr bufferPtr, out int bytesRead);
}