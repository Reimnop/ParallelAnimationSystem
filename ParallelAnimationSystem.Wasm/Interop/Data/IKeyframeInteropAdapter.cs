using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Wasm.Interop.Data;

// adapter for fixed-size keyframe value types
public interface IKeyframeInteropAdapter<T> where T : IKeyframe
{
    int Size { get; }
    void Write(T keyframe, IntPtr bufferPtr);
    T Read(IntPtr bufferPtr);
}