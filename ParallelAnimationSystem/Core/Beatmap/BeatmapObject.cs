using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ParallelAnimationSystem.Core.Beatmap;

/// <summary>
/// Represents a single object node in a beatmap.
/// </summary>
public class BeatmapObject(string id, BeatmapObjectData data) : INotifyPropertyChanged
{
    public static BeatmapObject DefaultRoot => new(
        string.Empty,
        new BeatmapObjectData([], [], [], []));
    
    [ThreadStatic]
    private static Stack<BeatmapObject>? traverseStack;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<BeatmapObject>? ChildAdded;
    public event EventHandler<BeatmapObject>? ChildRemoved;
    
    /// <summary>
    /// The unique identifier for this beatmap object.
    /// </summary>
    public string Id => id;

    /// <summary>
    /// The object's name.
    /// </summary>
    public string Name
    {
        get => name;
        set => SetField(ref name, value);
    }

    /// <summary>
    /// The data associated with this beatmap object.
    /// </summary>
    public BeatmapObjectData Data => data;

    /// <summary>
    /// The object's parent.
    /// </summary>
    public BeatmapObject? Parent
    {
        get => parent;
        set
        {
            var previousParent = parent;
            
            if (SetField(ref parent, value))
            {
                // Remove this object from the previous parent's children
                previousParent?.RemoveChild(this);
                
                // Add this object to the new parent's children
                value?.AddChild(this);
            }
        }
    }

    /// <summary>
    /// The object's children.
    /// </summary>
    public IReadOnlyList<BeatmapObject> Children => children;
    
    private string name = string.Empty;

    private BeatmapObject? parent = null;
    private readonly List<BeatmapObject> children = [];
    
    // ONLY adds children, does NOT set the parent!
    private void AddChild(BeatmapObject child)
    {
        if (children.Contains(child))
            return;
        
        children.Add(child);
        ChildAdded?.Invoke(this, child);
    }
    
    // ONLY removes children, does NOT set the parent!
    private void RemoveChild(BeatmapObject child)
    {
        if (!children.Remove(child))
            return;
        
        ChildRemoved?.Invoke(this, child);
    }
    
    /// <summary>
    /// Depth-first traversal of the beatmap object tree, applying the specified action to each object.
    /// </summary>
    public void Traverse(Action<BeatmapObject> action)
    {
        traverseStack ??= new Stack<BeatmapObject>();
        traverseStack.Push(this);
        while (traverseStack.Count > 0)
        {
            var current = traverseStack.Pop();
            action(current);
            
            foreach (var child in current.Children)
                traverseStack.Push(child);
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}