using System.Runtime.InteropServices;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Wasm.Interop.Data;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropKeyframeList
{
    [UnmanagedCallersOnly(EntryPoint = "keyframeList_getCount")]
    public static int GetCount(IntPtr ptr)
    {
        var wrapper = InteropHelper.IntPtrToObject<IKeyframeListInteropWrapper>(ptr);
        return wrapper.Count;
    }

    [UnmanagedCallersOnly(EntryPoint = "keyframeList_at")]
    public static IntPtr At(IntPtr ptr, int index)
    {
        var wrapper = InteropHelper.IntPtrToObject<IKeyframeListInteropWrapper>(ptr);
        var item = wrapper[index];
        return InteropHelper.ObjectToIntPtr(item);
    }

    [UnmanagedCallersOnly(EntryPoint = "keyframeList_add")]
    public static void Add(IntPtr ptr, IntPtr keyframePtr)
    {
        var wrapper = InteropHelper.IntPtrToObject<IKeyframeListInteropWrapper>(ptr);
        var keyframe = InteropHelper.IntPtrToObject<IKeyframe>(keyframePtr);
        wrapper.Add(keyframe);
    }

    [UnmanagedCallersOnly(EntryPoint = "keyframeList_removeAt")]
    public static void RemoveAt(IntPtr ptr, int index)
    {
        var wrapper = InteropHelper.IntPtrToObject<IKeyframeListInteropWrapper>(ptr);
        wrapper.RemoveAt(index);
    }

    [UnmanagedCallersOnly(EntryPoint = "keyframeList_replace")]
    public static unsafe void Replace(IntPtr ptr, IntPtr* keyframePtrsPtr, int keyframePtrCount)
    {
        var wrapper = InteropHelper.IntPtrToObject<IKeyframeListInteropWrapper>(ptr);

        if (keyframePtrsPtr != null && keyframePtrCount != 0)
        {
            // get all the keyframes
            var keyframes = new List<IKeyframe>(keyframePtrCount);
            for (var i = 0; i < keyframePtrCount; i++)
            {
                var keyframePtr = Marshal.PtrToStructure<IntPtr>(keyframePtrsPtr[i]);
                var keyframe = InteropHelper.IntPtrToObject<IKeyframe>(keyframePtr);
                keyframes.Add(keyframe);
            }
        
            // replace
            wrapper.Replace(keyframes);
        }
        else
        {
            wrapper.Replace([]);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "keyframeList_getIterator")]
    public static IntPtr GetIterator(IntPtr ptr)
    {
        var wrapper = InteropHelper.IntPtrToObject<IKeyframeListInteropWrapper>(ptr);
        // ReSharper disable once GenericEnumeratorNotDisposed
        var enumerator = wrapper.GetEnumerator();
        return InteropHelper.ObjectToIntPtr(enumerator);
    }

    [UnmanagedCallersOnly(EntryPoint = "keyframeList_iterator_moveNext")]
    public static bool IteratorMoveNext(IntPtr enumeratorPtr)
    {
        var enumerator = InteropHelper.IntPtrToObject<IEnumerator<IKeyframe>>(enumeratorPtr);
        return enumerator.MoveNext();
    }

    [UnmanagedCallersOnly(EntryPoint = "keyframeList_iterator_reset")]
    public static void IteratorReset(IntPtr enumeratorPtr)
    {
        var enumerator = InteropHelper.IntPtrToObject<IEnumerator<IKeyframe>>(enumeratorPtr);
        enumerator.Reset();
    }
    
    [UnmanagedCallersOnly(EntryPoint = "keyframeList_iterator_getCurrent")]
    public static IntPtr IteratorGetCurrent(IntPtr enumeratorPtr)
    {
        var enumerator = InteropHelper.IntPtrToObject<IEnumerator<IKeyframe>>(enumeratorPtr);
        return InteropHelper.ObjectToIntPtr(enumerator.Current);
    }
    
    [UnmanagedCallersOnly(EntryPoint = "keyframeList_iterator_dispose")]
    public static void IteratorDispose(IntPtr enumeratorPtr)
    {
        var enumerator = InteropHelper.IntPtrToObject<IEnumerator<IKeyframe>>(enumeratorPtr);
        if (enumerator is IDisposable disposable)
            disposable.Dispose();
    }
}