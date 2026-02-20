using ParallelAnimationSystem.Core.Data;

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

public class KeyframeListInteropWrapper<T>(KeyframeList<T> keyframeList, IKeyframeInteropAdapter<T> adapter): IKeyframeListInteropWrapper where T : IKeyframe
{
    public int Count => keyframeList.Count;

    public int GetKeyframeSize(int index)
        => adapter.Size;

    public void FetchAt(IntPtr bufferPtr, int index)
        => adapter.Write(keyframeList[index], bufferPtr);

    public int GetBufferSize(int start, int count)
        => adapter.Size * count;

    public void FetchRange(IntPtr bufferPtr, int start, int count)
    {
        for (var i = 0; i < count; i++)
            adapter.Write(keyframeList[start + i], bufferPtr + i * adapter.Size);
    }

    public void Load(IntPtr bufferPtr, int count)
    {
        var list = new List<T>(count);
        for (var i = 0; i < count; i++)
            list.Add(adapter.Read(bufferPtr + i * adapter.Size));
        keyframeList.Load(list);
    }
}