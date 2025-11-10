using System.ComponentModel;

namespace ParallelAnimationSystem.Core.Beatmap;

public class PrefabInstanceObject : IDisposable
{
    public event EventHandler<PrefabInstanceObject>? StartTimeChanged;
    public event EventHandler<PrefabInstanceObject>? KillTimeChanged;
    
    public float StartTime
    {
        get => timeline.StartTimeOffset;
        set
        {
            if (timeline.StartTimeOffset == value)
                return;
            
            timeline.StartTimeOffset = value;
            killTimeDirty = true;
            
            StartTimeChanged?.Invoke(this, this);
            KillTimeChanged?.Invoke(this, this);
        }
    }

    public float KillTime
    {
        get
        {
            if (!killTimeDirty)
                return cachedKillTime;

            cachedKillTime = CalculateKillTime();
            killTimeDirty = false;
            return cachedKillTime;
        }
    }
    
    private float cachedKillTime;
    
    private bool killTimeDirty = true;

    private readonly Timeline timeline;

    public PrefabInstanceObject(Prefab prefab)
    {
        var rootObject = prefab.RootObject;
        timeline = new Timeline(rootObject);
        
        // Subscribe to root object changes to invalidate kill time
        rootObject.Traverse(obj =>
        {
            obj.ChildAdded += OnBeatmapObjectChildAdded;
            obj.ChildRemoved += OnBeatmapObjectChildRemoved;
            obj.Data.PropertyChanged += OnBeatmapObjectDataPropertyChanged;
        });
    }

    ~PrefabInstanceObject()
    {
        Dispose(false);
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    private void Dispose(bool disposing)
    {
        if (!disposing)
            Console.Error.WriteLine("PrefabInstanceObject finalizer called without disposing. Did you forget to call Dispose()?");
        
        // Unsubscribe from root object changes
        timeline.RootObject.Traverse(obj =>
        {
            obj.ChildAdded -= OnBeatmapObjectChildAdded;
            obj.ChildRemoved -= OnBeatmapObjectChildRemoved;
            obj.Data.PropertyChanged -= OnBeatmapObjectDataPropertyChanged;
        });
    }

    private void OnBeatmapObjectChildAdded(object? sender, BeatmapObject e)
    {
        e.Traverse(obj =>
        {
            obj.ChildAdded += OnBeatmapObjectChildAdded;
            obj.ChildRemoved += OnBeatmapObjectChildRemoved;
            obj.Data.PropertyChanged += OnBeatmapObjectDataPropertyChanged;
        });
    }

    private void OnBeatmapObjectChildRemoved(object? sender, BeatmapObject e)
    {
        e.Traverse(obj =>
        {
            obj.ChildAdded -= OnBeatmapObjectChildAdded;
            obj.ChildRemoved -= OnBeatmapObjectChildRemoved;
            obj.Data.PropertyChanged -= OnBeatmapObjectDataPropertyChanged;
        });
    }

    private void OnBeatmapObjectDataPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(BeatmapObjectData.StartTime) or nameof(BeatmapObjectData.KillTimeOffset) or nameof(BeatmapObjectData.AutoKillType))
        {
            killTimeDirty = true;
            KillTimeChanged?.Invoke(this, this);
        }
    }

    private float CalculateKillTime()
    {
        var killTime = StartTime; // Lower bound
        timeline.RootObject.Traverse(obj =>
        {
            killTime = Math.Max(killTime, obj.Data.CalculateKillTime(StartTime));
        });
        return killTime;
    }
}