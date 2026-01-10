using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core.Beatmap;

public class PrefabsContainer : IDisposable
{
    public class PrefabInstancePrefabChangedEventArgs(PrefabInstance prefabInstance, Prefab? oldPrefab, Prefab? newPrefab) : EventArgs
    {
        public PrefabInstance PrefabInstance => prefabInstance;
        public Prefab? OldPrefab => oldPrefab;
        public Prefab? NewPrefab => newPrefab;
    }
    
    private class PrefabInstanceNode : IIndexedObject, IDisposable
    {
        public event EventHandler? KillTimeChanged; 
        
        public ObjectId Id => PrefabInstance.Id;
        
        public PrefabInstance PrefabInstance { get; }

        public int? Prefab
        {
            get => prefabNumericId;
            set
            {
                if (prefabNumericId != value)
                {
                    UnloadPrefab(prefabNumericId);
                    prefabNumericId = value;
                    LoadPrefab(prefabNumericId);
                    
                    killTimeDirty = true;
                }
            }
        }

        public float KillTime
        {
            get
            {
                if (!killTimeDirty) 
                    return cachedKillTime;
                
                killTimeDirty = false;
                cachedKillTime = CalculateKillTime();
                return cachedKillTime;
            }
        }

        private float cachedKillTime = float.NegativeInfinity;
        private bool killTimeDirty = false;
        private int? prefabNumericId = null;

        private readonly PrefabsContainer container;

        public PrefabInstanceNode(PrefabInstance prefabInstance, PrefabsContainer container)
        {
            PrefabInstance = prefabInstance;
            this.container = container;
            
            // Subscribe to events
            PrefabInstance.PropertyChanged += OnPrefabInstancePropertyChanged;
        }
        
        public void Dispose()
        {
            // Unsubscribe from events
            PrefabInstance.PropertyChanged -= OnPrefabInstancePropertyChanged;
            
            // Unload prefab
            UnloadPrefab(prefabNumericId);
        }

        private void OnPrefabInstancePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PrefabInstance.StartTime))
            {
                killTimeDirty = true;
                KillTimeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void LoadPrefab(int? prefabNumericId)
        {
            var prefab = GetPrefab(prefabNumericId);
            if (prefab is null)
                return;
            
            // Subscribe to events
            var beatmapObjects = prefab.BeatmapObjects;
            beatmapObjects.BeatmapObjectAdded += OnPrefabBeatmapObjectAdded;
            beatmapObjects.BeatmapObjectRemoved += OnPrefabBeatmapObjectRemoved;
            
            foreach (var obj in beatmapObjects)
                obj.PropertyChanged += OnPrefabBeatmapObjectPropertyChanged;
        }
        
        private void UnloadPrefab(int? prefabNumericId)
        {
            var prefab = GetPrefab(prefabNumericId);
            if (prefab is null)
                return;
            
            // Unsubscribe from events
            var beatmapObjects = prefab.BeatmapObjects;
            beatmapObjects.BeatmapObjectAdded -= OnPrefabBeatmapObjectAdded;
            beatmapObjects.BeatmapObjectRemoved -= OnPrefabBeatmapObjectRemoved;
            
            foreach (var obj in beatmapObjects)
                obj.PropertyChanged -= OnPrefabBeatmapObjectPropertyChanged;
        }

        private void OnPrefabBeatmapObjectAdded(object? sender, BeatmapObject e)
        {
            killTimeDirty = true;
            
            // Subscribe to property changed event
            e.PropertyChanged += OnPrefabBeatmapObjectPropertyChanged;
            
            KillTimeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnPrefabBeatmapObjectRemoved(object? sender, BeatmapObject e)
        {
            killTimeDirty = true;
            
            // Unsubscribe from property changed event
            e.PropertyChanged -= OnPrefabBeatmapObjectPropertyChanged;
            
            KillTimeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnPrefabBeatmapObjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(BeatmapObject.StartTime) or nameof(BeatmapObject.KillTimeOffset) or nameof(BeatmapObject.AutoKillType))
            {
                killTimeDirty = true;
                KillTimeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private float CalculateKillTime()
        {
            var prefab = GetPrefab(Prefab);
            if (prefab is null)
                return float.NegativeInfinity;
            
            return prefab.CalculateKillTime(PrefabInstance.StartTime);
        }

        private Prefab? GetPrefab(int? prefabNumericId)
        {
            if (!prefabNumericId.HasValue)
                return null;
            
            if (!container.TryGetPrefabInstancePrefab(prefabNumericId.Value, out var prefab))
                return null;

            return prefab;
        }
    }
    
    private class PrefabNode(Prefab prefab) : IIndexedObject
    {
        public ObjectId Id => Prefab.Id;
        
        public Prefab Prefab { get; } = prefab;
        public List<int> PrefabInstances { get; } = [];
    }
    
    public event EventHandler<PrefabInstancePrefabChangedEventArgs>? PrefabInstancePrefabChanged;
    public event EventHandler<PrefabInstance>? PrefabInstanceKillTimeChanged;
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
    
    public void Dispose()
    {
        foreach (var prefabInstanceNode in prefabInstanceNodes)
            prefabInstanceNode.Dispose();
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
    {
        var node = prefabInstanceNodes.Add(numericId => new PrefabInstanceNode(factory(numericId), this));
        node.KillTimeChanged += OnPrefabInstanceKillTimeChanged;
        return node.PrefabInstance;
    }

    private void OnPrefabInstanceKillTimeChanged(object? sender, EventArgs e)
    {
        if (sender is not PrefabInstanceNode node)
            return;
        
        PrefabInstanceKillTimeChanged?.Invoke(this, node.PrefabInstance);
    }

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
        
        // Dispose prefab instance node
        prefabInstanceNode.Dispose();
        
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
    
    public bool TryGetPrefabInstanceKillTime(string id, out float killTime)
    {
        if (prefabInstanceNodes.TryGet(id, out var prefabInstanceNode))
        {
            killTime = prefabInstanceNode.KillTime;
            return true;
        }

        killTime = float.NegativeInfinity;
        return false;
    }
    
    public bool TryGetPrefabInstanceKillTime(int id, out float killTime)
    {
        if (prefabInstanceNodes.TryGet(id, out var prefabInstanceNode))
        {
            killTime = prefabInstanceNode.KillTime;
            return true;
        }

        killTime = float.NegativeInfinity;
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