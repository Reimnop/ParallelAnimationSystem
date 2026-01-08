using System.Diagnostics.CodeAnalysis;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core.Beatmap;

public class PrefabsContainer
{
    public class PrefabInstancePrefabChangedEventArgs(PrefabInstance prefabInstance, Prefab? oldPrefab, Prefab? newPrefab) : EventArgs
    {
        public PrefabInstance PrefabInstance => prefabInstance;
        public Prefab? OldPrefab => oldPrefab;
        public Prefab? NewPrefab => newPrefab;
    }
    
    private class PrefabInstanceNode(PrefabInstance prefabInstance) : IIndexedObject
    {
        public ObjectId Id => PrefabInstance.Id;
        
        public PrefabInstance PrefabInstance { get; } = prefabInstance;
        public int? Prefab { get; set; } = null;
    }
    
    private class PrefabNode(Prefab prefab) : IIndexedObject
    {
        public ObjectId Id => Prefab.Id;
        
        public Prefab Prefab { get; } = prefab;
        public List<int> PrefabInstances { get; } = [];
    }
    
    public event EventHandler<PrefabInstancePrefabChangedEventArgs>? PrefabInstancePrefabChanged;
    public event EventHandler<PrefabInstance>? PrefabInstanceAdded;
    public event EventHandler<PrefabInstance>? PrefabInstanceRemoved;
    public event EventHandler<Prefab>? PrefabAdded;
    public event EventHandler<Prefab>? PrefabRemoved;

    public IReadOnlyCollection<PrefabInstance> PrefabInstances { get; }
    public IReadOnlyCollection<Prefab> Prefabs { get; }

    private readonly IndexedCollection<PrefabInstanceNode> prefabInstanceNodes = new();
    private readonly IndexedCollection<PrefabNode> prefabNodes = new();
    
    public PrefabsContainer()
    {
        PrefabInstances = prefabInstanceNodes.AdaptType(x => x.PrefabInstance);
        Prefabs = prefabNodes.AdaptType(x => x.Prefab);

        prefabInstanceNodes.ItemAdded += (_, node) => PrefabInstanceAdded?.Invoke(this, node.PrefabInstance);
        prefabInstanceNodes.ItemRemoved += (_, node) => PrefabInstanceRemoved?.Invoke(this, node.PrefabInstance);
        prefabNodes.ItemAdded += (_, node) => PrefabAdded?.Invoke(this, node.Prefab);
        prefabNodes.ItemRemoved += (_, node) => PrefabRemoved?.Invoke(this, node.Prefab);
    }
    
    public Prefab AddPrefab(IndexedItemFactory<Prefab> factory)
        => prefabNodes.Add(numericId => new PrefabNode(factory(numericId))).Prefab;
    
    public bool RemovePrefab(string id)
    {
        if (!prefabNodes.TryConvertStringIdToNumericId(id, out var numericId))
            return false;
        
        return RemovePrefab(numericId);
    }

    public bool RemovePrefab(int numericId)
    {
        if (!prefabNodes.TryGet(numericId, out var prefabNode))
            return false;
        
        // Remove prefab from all associated prefab instances
        foreach (var prefabInstanceNumericId in prefabNode.PrefabInstances)
        {
            if (!prefabInstanceNodes.TryGet(prefabInstanceNumericId, out var prefabInstanceNode))
                continue;
            
            SetPrefabInstancePrefab(prefabInstanceNode.PrefabInstance.Id.String, null);
        }
        
        return prefabNodes.Remove(numericId);
    }

    public PrefabInstance AddPrefabInstance(IndexedItemFactory<PrefabInstance> factory)
        => prefabInstanceNodes.Add(numericId => new PrefabInstanceNode(factory(numericId))).PrefabInstance;

    public bool RemovePrefabInstance(string id)
    {
        if (!prefabInstanceNodes.TryConvertStringIdToNumericId(id, out var numericId))
            return false;

        return RemovePrefabInstance(numericId);
    }

    public bool RemovePrefabInstance(int numericId)
    {
        if (!prefabInstanceNodes.TryGet(numericId, out var prefabInstanceNode))
            return false;
        
        // Get prefab node
        var prefabNumericId = prefabInstanceNode.Prefab;
        if (prefabNumericId.HasValue && prefabNodes.TryGet(prefabNumericId.Value, out var prefabNode))
        {
            // Remove from prefab's instances list
            prefabNode.PrefabInstances.Remove(numericId);
        }
        
        return prefabInstanceNodes.Remove(numericId);
    }
    
    public void SetPrefabInstancePrefab(string prefabInstanceId, string? prefabId)
    {
        var prefabInstanceNode = prefabInstanceNodes[prefabInstanceId];

        PrefabNode? newPrefabNode = null;
        if (prefabId is not null)
            newPrefabNode = prefabNodes[prefabId];
        
        var oldPrefabNode = prefabInstanceNode.Prefab.HasValue
            ? (prefabNodes.TryGet(prefabInstanceNode.Prefab.Value, out var oldPrefab2) ? oldPrefab2 : null)
            : null;
        
        if (oldPrefabNode == newPrefabNode)
            return;
        
        // Remove from old prefab's instances list
        oldPrefabNode?.PrefabInstances.Remove(prefabInstanceNode.PrefabInstance.Id.Numeric);

        // Add to new prefab's instances list
        newPrefabNode?.PrefabInstances.Add(prefabInstanceNode.PrefabInstance.Id.Numeric);

        prefabInstanceNode.Prefab = newPrefabNode?.Id.Numeric;
        
        PrefabInstancePrefabChanged?.Invoke(this, new PrefabInstancePrefabChangedEventArgs(
            prefabInstanceNode.PrefabInstance,
            oldPrefabNode?.Prefab,
            newPrefabNode?.Prefab));
    }
    
    public bool TryGetPrefabInstancePrefab(string id, out Prefab? prefab)
    {
        prefab = null;
        
        if (prefabInstanceNodes.TryGet(id, out var prefabInstanceNode))
        {
            if (prefabInstanceNode.Prefab.HasValue && prefabNodes.TryGet(prefabInstanceNode.Prefab.Value, out var prefabNode))
                prefab = prefabNode.Prefab;
            return true;
        }
        
        return false;
    }
    
    public bool TryGetPrefabInstancePrefab(int id, out Prefab? prefab)
    {
        prefab = null;
        
        if (prefabInstanceNodes.TryGet(id, out var prefabInstanceNode))
        {
            if (prefabInstanceNode.Prefab.HasValue && prefabNodes.TryGet(prefabInstanceNode.Prefab.Value, out var prefabNode))
                prefab = prefabNode.Prefab;
            return true;
        }
        
        return false;
    }
    
    public bool TryGetPrefab(string id, [MaybeNullWhen(false)] out Prefab prefab)
    {
        if (prefabNodes.TryGet(id, out var prefabNode))
        {
            prefab = prefabNode.Prefab;
            return true;
        }

        prefab = null;
        return false;
    }
    
    public bool TryGetPrefab(int id, [MaybeNullWhen(false)] out Prefab prefab)
    {
        if (prefabNodes.TryGet(id, out var prefabNode))
        {
            prefab = prefabNode.Prefab;
            return true;
        }

        prefab = null;
        return false;
    }
    
    public bool TryGetPrefabInstance(string id, [MaybeNullWhen(false)] out PrefabInstance prefabInstance)
    {
        if (prefabInstanceNodes.TryGet(id, out var prefabInstanceNode))
        {
            prefabInstance = prefabInstanceNode.PrefabInstance;
            return true;
        }

        prefabInstance = null;
        return false;
    }
    
    public bool TryGetPrefabInstance(int id, [MaybeNullWhen(false)] out PrefabInstance prefabInstance)
    {
        if (prefabInstanceNodes.TryGet(id, out var prefabInstanceNode))
        {
            prefabInstance = prefabInstanceNode.PrefabInstance;
            return true;
        }

        prefabInstance = null;
        return false;
    }
}