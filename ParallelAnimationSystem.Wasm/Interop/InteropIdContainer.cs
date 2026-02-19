using System.Runtime.InteropServices;
using ParallelAnimationSystem.Core.Model;
using ParallelAnimationSystem.Wasm.Interop.Data;

namespace ParallelAnimationSystem.Wasm.Interop;

public static class InteropIdContainer
{
    [UnmanagedCallersOnly(EntryPoint = "idContainer_getCount")]
    public static int GetCount(IntPtr ptr)
    {
        var wrapper = InteropHelper.IntPtrToObject<IIdContainerInteropWrapper>(ptr);
        return wrapper.Count;
    }

    [UnmanagedCallersOnly(EntryPoint = "idContainer_getById")]
    public static IntPtr GetById(IntPtr ptr, IntPtr idPtr)
    {
        var wrapper = InteropHelper.IntPtrToObject<IIdContainerInteropWrapper>(ptr);
        var id = Marshal.PtrToStringUTF8(idPtr);
        if (id is null)            
            return IntPtr.Zero;
        var item = wrapper.GetById(id);
        if (item is null)
            return IntPtr.Zero;
        return InteropHelper.ObjectToIntPtr(item);
    }

    [UnmanagedCallersOnly(EntryPoint = "idContainer_insert")]
    public static bool Insert(IntPtr ptr, IntPtr itemPtr)
    {
        var wrapper = InteropHelper.IntPtrToObject<IIdContainerInteropWrapper>(ptr);
        var item = InteropHelper.IntPtrToObject<IStringIdentifiable>(itemPtr);
        return wrapper.Insert(item);
    }

    [UnmanagedCallersOnly(EntryPoint = "idContainer_remove")]
    public static bool Remove(IntPtr ptr, IntPtr idPtr)
    {
        var wrapper = InteropHelper.IntPtrToObject<IIdContainerInteropWrapper>(ptr);
        var id = Marshal.PtrToStringUTF8(idPtr);
        if (id is null)
            return false;
        return wrapper.Remove(id);
    }

    [UnmanagedCallersOnly(EntryPoint = "idContainer_getIterator")]
    public static IntPtr GetIterator(IntPtr ptr)
    {
        var wrapper = InteropHelper.IntPtrToObject<IIdContainerInteropWrapper>(ptr);
        // ReSharper disable once GenericEnumeratorNotDisposed
        var enumerator = wrapper.GetEnumerator();
        return InteropHelper.ObjectToIntPtr(enumerator);
    }

    [UnmanagedCallersOnly(EntryPoint = "idContainer_iterator_moveNext")]
    public static bool IteratorMoveNext(IntPtr enumeratorPtr)
    {
        var enumerator = InteropHelper.IntPtrToObject<IEnumerator<KeyValuePair<string, IStringIdentifiable>>>(enumeratorPtr);
        return enumerator.MoveNext();
    }

    [UnmanagedCallersOnly(EntryPoint = "idContainer_iterator_reset")]
    public static void IteratorReset(IntPtr enumeratorPtr)
    {
        var enumerator = InteropHelper.IntPtrToObject<IEnumerator<KeyValuePair<string, IStringIdentifiable>>>(enumeratorPtr);
        enumerator.Reset();
    }
    
    [UnmanagedCallersOnly(EntryPoint = "idContainer_iterator_getCurrent_key")]
    public static IntPtr IteratorGetCurrentKey(IntPtr enumeratorPtr)
    {
        var enumerator = InteropHelper.IntPtrToObject<IEnumerator<KeyValuePair<string, IStringIdentifiable>>>(enumeratorPtr);
        return InteropHelper.ObjectToIntPtr(enumerator.Current.Key);
    }

    [UnmanagedCallersOnly(EntryPoint = "idContainer_iterator_getCurrent_value")]
    public static IntPtr IteratorGetCurrentValue(IntPtr enumeratorPtr)
    {
        var enumerator = InteropHelper.IntPtrToObject<IEnumerator<KeyValuePair<string, IStringIdentifiable>>>(enumeratorPtr);
        return InteropHelper.ObjectToIntPtr(enumerator.Current.Value);
    }

    [UnmanagedCallersOnly(EntryPoint = "idContainer_iterator_dispose")]
    public static void IteratorDispose(IntPtr enumeratorPtr)
    {
        var enumerator = InteropHelper.IntPtrToObject<IEnumerator<KeyValuePair<string, IStringIdentifiable>>>(enumeratorPtr);
        if (enumerator is IDisposable disposable)
            disposable.Dispose();
    }
}