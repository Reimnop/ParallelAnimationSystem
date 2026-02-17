using System.Collections;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Wasm.Interop.Data;

public interface IKeyframeListInteropWrapper : IEnumerable<IKeyframe>
{
    int Count { get; }
    IKeyframe this[int index] { get; }
    void Add(IKeyframe keyframe);
    void RemoveAt(int index);
    void Replace(IEnumerable<IKeyframe> collection);
}

public class KeyframeListInteropWrapper<T>(KeyframeList<T> keyframeList): IKeyframeListInteropWrapper where T : IKeyframe
{
    public int Count => keyframeList.Count;
    
    public IKeyframe this[int index] => keyframeList[index];
    
    public void Add(IKeyframe keyframe)
    {
        keyframeList.Add((T)keyframe);
    }

    public void RemoveAt(int index)
    {
        keyframeList.RemoveAt(index);
    }

    public void Replace(IEnumerable<IKeyframe> collection)
    {
        keyframeList.Replace(collection.Cast<T>());
    }
    
    public IEnumerator<IKeyframe> GetEnumerator()
    {
        foreach (var keyframe in keyframeList)
            yield return keyframe;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}