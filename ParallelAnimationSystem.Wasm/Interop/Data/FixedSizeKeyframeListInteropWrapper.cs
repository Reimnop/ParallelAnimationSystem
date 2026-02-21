using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Wasm.Interop.Data;

public class FixedSizeKeyframeListInteropWrapper<T>(KeyframeList<T> keyframeList, IFixedSizeKeyframeInteropCodec<T> codec): IKeyframeListInteropWrapper where T : IKeyframe
{
    public int Count => keyframeList.Count;

    public int GetKeyframeSize(int index)
        => codec.Size;

    public void FetchAt(IntPtr bufferPtr, int index)
        => codec.Write(keyframeList[index], bufferPtr);

    public int GetBufferSize(int start, int count)
        => codec.Size * count;

    public void FetchRange(IntPtr bufferPtr, int start, int count)
    {
        for (var i = 0; i < count; i++)
            codec.Write(keyframeList[start + i], bufferPtr + i * codec.Size);
    }

    public void Load(IntPtr bufferPtr, int count)
    {
        var list = new List<T>(count);
        for (var i = 0; i < count; i++)
            list.Add(codec.Read(bufferPtr + i * codec.Size));
        keyframeList.Load(list);
    }
}