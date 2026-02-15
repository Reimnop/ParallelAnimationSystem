using System.Collections;
using System.Runtime.InteropServices;
using ParallelAnimationSystem.Wasm.Interop.Data;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropIdContainer
{
    [UnmanagedCallersOnly(EntryPoint = "idContainer_getCount")]
    public static int GetCount(IntPtr ptr)
    {
        var adapter = InteropHelper.IntPtrToObject<IInteropIdContainerAdapter>(ptr);
        return adapter.Count;
    }

    [UnmanagedCallersOnly(EntryPoint = "idContainer_getById")]
    public static IntPtr GetById(IntPtr ptr, IntPtr idPtr)
    {
        var adapter = InteropHelper.IntPtrToObject<IInteropIdContainerAdapter>(ptr);
        var id = Marshal.PtrToStringUTF8(idPtr);
        if (id is null)            
            return IntPtr.Zero;
        var item = adapter.GetById(id);
        if (item is null)
            return IntPtr.Zero;
        return InteropHelper.ObjectToIntPtr(item);
    }

    [UnmanagedCallersOnly(EntryPoint = "idContainer_insert")]
    public static bool Insert(IntPtr ptr, IntPtr itemPtr)
    {
        var adapter = InteropHelper.IntPtrToObject<IInteropIdContainerAdapter>(ptr);
        var item = InteropHelper.IntPtrToObject<object>(itemPtr);
        return adapter.Insert(item);
    }

    [UnmanagedCallersOnly(EntryPoint = "idContainer_remove")]
    public static bool Remove(IntPtr ptr, IntPtr idPtr)
    {
        var adapter = InteropHelper.IntPtrToObject<IInteropIdContainerAdapter>(ptr);
        var id = Marshal.PtrToStringUTF8(idPtr);
        if (id is null)
            return false;
        return adapter.Remove(id);
    }

    [UnmanagedCallersOnly(EntryPoint = "idContainer_getIterator")]
    public static IntPtr GetIterator(IntPtr ptr)
    {
        var adapter = InteropHelper.IntPtrToObject<IInteropIdContainerAdapter>(ptr);
        // ReSharper disable once GenericEnumeratorNotDisposed
        var enumerator = adapter.GetEnumerator();
        return InteropHelper.ObjectToIntPtr(enumerator);
    }

    [UnmanagedCallersOnly(EntryPoint = "idContainer_iterator_moveNext")]
    public static bool IteratorMoveNext(IntPtr enumeratorPtr)
    {
        var enumerator = InteropHelper.IntPtrToObject<IEnumerator>(enumeratorPtr);
        return enumerator.MoveNext();
    }
    
    [UnmanagedCallersOnly(EntryPoint = "idContainer_iterator_getCurrent_key")]
    public static IntPtr IteratorGetCurrentKey(IntPtr enumeratorPtr)
    {
        var enumerator = InteropHelper.IntPtrToObject<IEnumerator>(enumeratorPtr);
        if (enumerator.Current is not KeyValuePair<string, object> kvp)
            return IntPtr.Zero;
        return InteropHelper.StringToIntPtrUTF8(kvp.Key);
    }

    [UnmanagedCallersOnly(EntryPoint = "idContainer_iterator_getCurrent_value")]
    public static IntPtr IteratorGetCurrentValue(IntPtr enumeratorPtr)
    {
        var enumerator = InteropHelper.IntPtrToObject<IEnumerator>(enumeratorPtr);
        if (enumerator.Current is not KeyValuePair<string, object> kvp)
            return IntPtr.Zero;
        return InteropHelper.ObjectToIntPtr(kvp.Value);
    }

    [UnmanagedCallersOnly(EntryPoint = "idContainer_iterator_dispose")]
    public static void IteratorDispose(IntPtr enumeratorPtr)
    {
        var enumerator = InteropHelper.IntPtrToObject<IEnumerator>(enumeratorPtr);
        if (enumerator is IDisposable disposable)
            disposable.Dispose();
    }
}