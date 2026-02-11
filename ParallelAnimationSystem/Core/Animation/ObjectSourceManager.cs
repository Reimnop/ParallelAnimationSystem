using System.ComponentModel;
using System.Diagnostics;
using ParallelAnimationSystem.Core.Model;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core.Animation;

public class ObjectSourceManager(PlaybackObjectContainer playbackObjects) : IDisposable
{
    private readonly MainObjectSource mainObjectSource = new(playbackObjects);
    private readonly Dictionary<string, PrefabInstanceObjectSource> prefabInstanceObjectSources = [];
    private readonly Dictionary<string, HashSet<string>> instanceIdsByPrefabId = [];

    private BeatmapData? attachedBeatmapData;
    
    public void Dispose()
    {
        mainObjectSource.Dispose();
    }

    public void AttachBeatmapData(BeatmapData beatmapData)
    {
        if (attachedBeatmapData is not null)
            throw new InvalidOperationException($"A {nameof(BeatmapData)} is already attached");
        
        attachedBeatmapData = beatmapData;
        
        // initialize main object source
        mainObjectSource.AttachBeatmapData(attachedBeatmapData);
        
        // initialize prefab instance object sources
        foreach (var (id, instance) in beatmapData.PrefabInstances)
        {
            var prefabInstanceObjectSource = new PrefabInstanceObjectSource(id, playbackObjects)
            {
                StartTime = instance.StartTime,
                Position = instance.Position,
                Scale = instance.Scale,
                Rotation = MathUtil.DegreesToRadians(instance.Rotation),
            };
            prefabInstanceObjectSources.Add(id, prefabInstanceObjectSource);
            
            var prefabId = instance.PrefabId;
            if (prefabId is not null && beatmapData.Prefabs.TryGetValue(prefabId, out var prefab))
                prefabInstanceObjectSource.AttachPrefab(prefab);
            
            // push prefab id lookup
            if (prefabId is not null)
            {
                var idSet = instanceIdsByPrefabId.GetOrInsert(prefabId, () => []);
                idSet.Add(id);
            }
            
            instance.PropertyChanged += OnPrefabInstancePropertyChanged;
        }
        
        // attach events
        attachedBeatmapData.PrefabInstances.Inserted += OnPrefabInstanceInserted;
        attachedBeatmapData.PrefabInstances.Removed += OnPrefabInstanceRemoved;
        attachedBeatmapData.Prefabs.Inserted += OnPrefabInserted;
        attachedBeatmapData.Prefabs.Removed += OnPrefabRemoved;
    }

    public void DetachBeatmapData()
    {
        if (attachedBeatmapData is null)
            return;
        
        mainObjectSource.DetachBeatmapData();

        foreach (var (_, prefabInstanceObjectSource) in prefabInstanceObjectSources)
            prefabInstanceObjectSource.Dispose();
        
        prefabInstanceObjectSources.Clear();
        
        // detach events
        attachedBeatmapData.PrefabInstances.Inserted -= OnPrefabInstanceInserted;
        attachedBeatmapData.PrefabInstances.Removed -= OnPrefabInstanceRemoved;
        attachedBeatmapData.Prefabs.Inserted -= OnPrefabInserted;
        attachedBeatmapData.Prefabs.Removed -= OnPrefabRemoved;
        
        attachedBeatmapData = null;
    }

    private void OnPrefabInstanceInserted(object? sender, BeatmapPrefabInstance e)
    {
        var prefabInstanceObjectSource = new PrefabInstanceObjectSource(e.Id, playbackObjects)
        {
            StartTime = e.StartTime,
            Position = e.Position,
            Scale = e.Scale,
            Rotation = MathUtil.DegreesToRadians(e.Rotation),
        };
        prefabInstanceObjectSources.Add(e.Id, prefabInstanceObjectSource);
        
        Debug.Assert(attachedBeatmapData is not null);
        if (e.PrefabId is not null && attachedBeatmapData.Prefabs.TryGetValue(e.PrefabId, out var prefab))
            prefabInstanceObjectSource.AttachPrefab(prefab);
        
        if (e.PrefabId is not null)
        {
            var idSet = instanceIdsByPrefabId.GetOrInsert(e.PrefabId, () => []);
            idSet.Add(e.Id);
        }
        
        e.PropertyChanged += OnPrefabInstancePropertyChanged;
    }

    private void OnPrefabInstanceRemoved(object? sender, BeatmapPrefabInstance e)
    {
        if (prefabInstanceObjectSources.TryGetValue(e.Id, out var prefabInstanceObjectSource))
        {
            prefabInstanceObjectSource.Dispose();
            prefabInstanceObjectSources.Remove(e.Id);
        }
        
        if (e.PrefabId is not null && instanceIdsByPrefabId.TryGetValue(e.PrefabId, out var idSet))
        {
            idSet.Remove(e.Id);
            if (idSet.Count == 0)
                instanceIdsByPrefabId.Remove(e.PrefabId);
        }
        
        e.PropertyChanged -= OnPrefabInstancePropertyChanged;
    }

    private void OnPrefabInserted(object? sender, BeatmapPrefab e)
    {
        if (!instanceIdsByPrefabId.TryGetValue(e.Id, out var instanceIds))
            return;
        
        foreach (var instanceId in instanceIds)
        {
            if (prefabInstanceObjectSources.TryGetValue(instanceId, out var prefabInstanceObjectSource))
                prefabInstanceObjectSource.AttachPrefab(e);
        }
    }

    private void OnPrefabRemoved(object? sender, BeatmapPrefab e)
    {
        var instanceIds = instanceIdsByPrefabId.GetOrInsert(e.Id, () => []);
        foreach (var instanceId in instanceIds)
        {
            if (prefabInstanceObjectSources.TryGetValue(instanceId, out var prefabInstanceObjectSource))
                prefabInstanceObjectSource.DetachPrefab();
        }
    }

    private void OnPrefabInstancePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not BeatmapPrefabInstance instance)
            return;
        
        if (!prefabInstanceObjectSources.TryGetValue(instance.Id, out var prefabInstanceObjectSource))
            return;
        
        switch (e.PropertyName)
        {
            case nameof(BeatmapPrefabInstance.StartTime):
                prefabInstanceObjectSource.StartTime = instance.StartTime;
                break;
            case nameof(BeatmapPrefabInstance.Position):
                prefabInstanceObjectSource.Position = instance.Position;
                break;
            case nameof(BeatmapPrefabInstance.Scale):
                prefabInstanceObjectSource.Scale = instance.Scale;
                break;
            case nameof(BeatmapPrefabInstance.Rotation):
                prefabInstanceObjectSource.Rotation = MathUtil.DegreesToRadians(instance.Rotation);
                break;
            case nameof(BeatmapPrefabInstance.PrefabId):
                // detach old prefab
                if (prefabInstanceObjectSource.AttachedPrefab is not null)
                {
                    var oldPrefabId = prefabInstanceObjectSource.AttachedPrefab.Id;
                    var oldPrefabIdSet = instanceIdsByPrefabId.GetOrInsert(oldPrefabId, () => []);
                    oldPrefabIdSet.Remove(instance.Id);
                    
                    prefabInstanceObjectSource.DetachPrefab();
                }
                
                // attach new prefab
                Debug.Assert(attachedBeatmapData is not null);
                var prefabId = instance.PrefabId;
                if (prefabId is not null && attachedBeatmapData.Prefabs.TryGetValue(prefabId, out var prefab))
                    prefabInstanceObjectSource.AttachPrefab(prefab);
                
                if (prefabId is not null)
                {
                    var idSet = instanceIdsByPrefabId.GetOrInsert(prefabId, () => []);
                    idSet.Add(instance.Id);
                }
                break;
        }
    }
}