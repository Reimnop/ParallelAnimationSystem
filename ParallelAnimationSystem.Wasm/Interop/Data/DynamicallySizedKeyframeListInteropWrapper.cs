using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Wasm.Interop.Data;

public class DynamicSizeKeyframeListInteropWrapper<T>(KeyframeList<T> keyframeList, IDynamicSizeKeyframeInteropCodec<T> codec): IKeyframeListInteropWrapper where T : IKeyframe
{
    public int Count => keyframeList.Count;

    public int GetKeyframeSize(int index)
        => codec.GetSize(keyframeList[index]);

    public void FetchAt(IntPtr bufferPtr, int index)
        => codec.Write(keyframeList[index], bufferPtr);

    public int GetBufferSize(int start, int count)
    {
        var size = 0;
        for (var i = 0; i < count; i++)
            size += codec.GetSize(keyframeList[start + i]);
        return size;
    }

    public void FetchRange(IntPtr bufferPtr, int start, int count)
    {
        var currentPtr = bufferPtr;
        for (var i = 0; i < count; i++)
        {
            var bytesWritten = codec.Write(keyframeList[start + i], currentPtr);
            currentPtr += bytesWritten;
        }
    }

    public void Load(IntPtr bufferPtr, int count)
    {
        var currentPtr = bufferPtr;
        
        var list = new List<T>(count);
        for (var i = 0; i < count; i++)
        {
            var keyframe = codec.Read(currentPtr, out var bytesRead);
            list.Add(keyframe);
            currentPtr += bytesRead;
        }
        keyframeList.Load(list);
    }
}