using System.Collections;

namespace ParallelAnimationSystem.Wasm.Interop.Data;

public interface IInteropIdContainerAdapter : IEnumerable
{
    int Count { get; }
    
    object? GetById(string id);
    bool Insert(object? item);
    bool Remove(string id);
}