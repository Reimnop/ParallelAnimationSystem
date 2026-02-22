namespace ParallelAnimationSystem.Wasm.Interop.Data;

public interface IKeyframeListInteropWrapper
{
    int Count { get; }
    int GetKeyframeSize(int index);
    void FetchAt(IntPtr bufferPtr, int index);
    int GetBufferSize(int start, int count);
    void FetchRange(IntPtr bufferPtr, int start, int count);
    void Load(IntPtr bufferPtr, int count);
}
