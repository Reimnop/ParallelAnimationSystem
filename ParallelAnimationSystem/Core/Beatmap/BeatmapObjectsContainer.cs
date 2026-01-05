using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace ParallelAnimationSystem.Core.Beatmap;

// Numeric IDs are handled by the container
// So we need a factory to create objects
public delegate BeatmapObject BeatmapObjectFactory(int numericId);

public class BeatmapObjectsContainer : IEnumerable<BeatmapObject>
{
    private class BeatmapObjectNode(BeatmapObject obj)
    {
        public BeatmapObject Object { get; } = obj;
        public int? Parent { get; set; } = null;
        public List<int> Children { get; } = [];
    }
    
    public event EventHandler<BeatmapObject>? BeatmapObjectAdded;
    public event EventHandler<BeatmapObject>? BeatmapObjectRemoved;
    
    public int Count => count;

    private readonly List<BeatmapObjectNode?> beatmapObjectNodes = [];
    private readonly Dictionary<string, int> stringIdToNumericId = [];
    private int count = 0;

    public void Add(BeatmapObjectFactory factory)
    {
        var numericId = GetNextNumericId();
        var beatmapObject = factory(numericId);
        var node = new BeatmapObjectNode(beatmapObject);
        
        var stringId = beatmapObject.Id.String;
        
        // Check if there's already an object with the same ID
        if (stringIdToNumericId.ContainsKey(stringId))
            throw new InvalidOperationException($"Beatmap object with ID '{stringId}' already exists.");
        
        EnsureNodeListSize(numericId + 1);
        beatmapObjectNodes[numericId] = node;
        stringIdToNumericId[stringId] = numericId;
        count++;
        
        BeatmapObjectAdded?.Invoke(this, beatmapObject);
    }
    
    public bool Remove(string id)
    {
        if (!stringIdToNumericId.TryGetValue(id, out var numericId))
            return false;
        return Remove(numericId);
    }
    
    public bool Remove(int numericId)
    {
        if (numericId < 0 || numericId >= beatmapObjectNodes.Count)
            return false;
        var node = beatmapObjectNodes[numericId];
        if (node is null)
            return false;
        
        var stringId = node.Object.Id.String;
        stringIdToNumericId.Remove(stringId);
        
        // Clear children's parent references
        foreach (var child in node.Children)
        {
            var childNode = beatmapObjectNodes[child];
            if (childNode is not null)
                childNode.Parent = null;
        }
        
        beatmapObjectNodes[numericId] = null;
        count--;
        BeatmapObjectRemoved?.Invoke(this, node.Object);
        
        return true;
    }

    public void SetParent(string childId, string? parentId)
    {
        var childNode = TryGetNode(childId, out var cn) ? cn : null;
        if (childNode is null)
            ThrowObjectNotFound(childId);

        BeatmapObjectNode? parentNode = null;
        if (parentId is not null)
        {
            if (!TryGetNode(parentId, out parentNode))
                ThrowObjectNotFound(parentId);
        }

        var childNumericId = childNode.Object.Id.Int;
        
        // Remove from old parent
        if (childNode.Parent.HasValue)
        {
            var oldParentNode = beatmapObjectNodes[childNode.Parent.Value];
            oldParentNode?.Children.Remove(childNumericId);
        }
        
        // Set new parent
        if (parentNode is not null)
        {
            childNode.Parent = stringIdToNumericId[parentId!];
            parentNode.Children.Add(childNumericId);
        }
        else
        {
            childNode.Parent = null;
        }
    }

    public bool TryGetParent(string id, out BeatmapObject? parent)
    {
        if (!stringIdToNumericId.TryGetValue(id, out var numericId))
        {
            parent = null;
            return false;
        }
        
        return TryGetParent(numericId, out parent);
    }

    public bool TryGetParent(int numericId, out BeatmapObject? parent)
    {
        if (!TryGetNode(numericId, out var node))
        {
            parent = null;
            return false;
        }
        
        if (!node.Parent.HasValue)
        {
            parent = null;
            return true;
        }
        
        var parentNode = beatmapObjectNodes[node.Parent.Value];
        parent = parentNode?.Object;
        return true;
    }
    
    public bool TryGetChildren(string id, [MaybeNullWhen(false)] out List<BeatmapObject> children)
    {
        if (!stringIdToNumericId.TryGetValue(id, out var numericId))
        {
            children = null;
            return false;
        }
        
        return TryGetChildren(numericId, out children);
    }
    
    public bool TryGetChildren(int numericId, [MaybeNullWhen(false)] out List<BeatmapObject> children)
    {
        if (!TryGetNode(numericId, out var node))
        {
            children = null;
            return false;
        }

        children = node.Children
            .Select(childNumericId => beatmapObjectNodes[childNumericId]?.Object)
            .OfType<BeatmapObject>()
            .ToList();
        
        return true;
    }
    
    public bool TryGet(string id, [MaybeNullWhen(false)] out BeatmapObject beatmapObject)
    {
        if (!stringIdToNumericId.TryGetValue(id, out var numericId))
        {
            beatmapObject = null;
            return false;
        }
        
        return TryGet(numericId, out beatmapObject);
    }
    
    public bool TryGet(int numericId, [MaybeNullWhen(false)] out BeatmapObject beatmapObject)
    {
        if (numericId < 0 || numericId >= beatmapObjectNodes.Count)
        {
            beatmapObject = null;
            return false;
        }
        
        var node = beatmapObjectNodes[numericId];
        if (node is null)
        {
            beatmapObject = null;
            return false;
        }
        
        beatmapObject = node.Object;
        return true;
    }
    
    private bool TryGetNode(string id, [MaybeNullWhen(false)] out BeatmapObjectNode node)
    {
        if (!stringIdToNumericId.TryGetValue(id, out var numericId))
        {
            node = null;
            return false;
        }
        
        return TryGetNode(numericId, out node);
    }
    
    private bool TryGetNode(int numericId, [MaybeNullWhen(false)] out BeatmapObjectNode node)
    {
        if (numericId < 0 || numericId >= beatmapObjectNodes.Count)
        {
            node = null;
            return false;
        }
        
        node = beatmapObjectNodes[numericId];
        return node is not null;
    }
    
    public bool Contains(string id)
        => stringIdToNumericId.ContainsKey(id);
    
    public bool Contains(int numericId)
        => numericId >= 0 && numericId < beatmapObjectNodes.Count && beatmapObjectNodes[numericId] is not null;
    
    private int GetNextNumericId()
        => beatmapObjectNodes.Count;

    private void EnsureNodeListSize(int size)
    {
        while (beatmapObjectNodes.Count < size)
            beatmapObjectNodes.Add(null);
    }

    public IEnumerator<BeatmapObject> GetEnumerator()
    {
        foreach (var node in beatmapObjectNodes)
        {
            if (node is not null)
                yield return node.Object;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
    [DoesNotReturn]
    private static void ThrowObjectNotFound(string id)
        => throw new KeyNotFoundException($"Beatmap object with ID '{id}' not found.");
}