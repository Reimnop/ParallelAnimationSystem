using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Wasm.Interop.Data;

// adapter for fixed-size keyframe value types
public interface IKeyframeInteropAdapter<T> where T : IKeyframe
{
    int Size { get; }
    void ToBytes(T keyframe, IntPtr bufferPtr);
    T FromBytes(IntPtr bufferPtr);
}