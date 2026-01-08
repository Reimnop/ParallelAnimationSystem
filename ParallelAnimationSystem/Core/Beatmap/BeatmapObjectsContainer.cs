using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace ParallelAnimationSystem.Core.Beatmap;

public class BeatmapObjectsContainer : IReadOnlyCollection<BeatmapObject>
{
    private class BeatmapObjectNode(BeatmapObject obj) : IIndexedObject
    {
        public ObjectId Id => Object.Id;
        
        public BeatmapObject Object { get; } = obj;
        public int? Parent { get; set; } = null;
        public List<int> Children { get; } = [];
    }
    
    public event EventHandler<BeatmapObject>? BeatmapObjectAdded;
    public event EventHandler<BeatmapObject>? BeatmapObjectRemoved;
    
    public int Count => objectNodes.Count;

    private readonly IndexedCollection<BeatmapObjectNode> objectNodes = new();

    public BeatmapObjectsContainer()
    {
        objectNodes.ItemAdded += (_, node) => BeatmapObjectAdded?.Invoke(this, node.Object);
        objectNodes.ItemRemoved += (_, node) => BeatmapObjectRemoved?.Invoke(this, node.Object);
    }

    public BeatmapObject Add(IndexedItemFactory<BeatmapObject> factory)
        => objectNodes.Add(numericId => new BeatmapObjectNode(factory(numericId))).Object;

    public bool Remove(string id)
    {
        if (!objectNodes.TryConvertStringIdToNumericId(id, out var numericId))
            return false;
        
        return Remove(numericId);
    }
    
    public bool Remove(int numericId)
    {
        if (!objectNodes.TryGet(numericId, out var node))
            return false;
        
        // Remove children's parent references
        foreach (var childNumericId in node.Children)
        {
            if (!objectNodes.TryGet(childNumericId, out var childNode))
                continue;
            
            childNode.Parent = null;
        }
        
        // Remove from parent's children list
        if (node.Parent.HasValue)
        {
            var parentNumericId = node.Parent.Value;
            if (objectNodes.TryGet(parentNumericId, out var parentNode))
                parentNode.Children.Remove(numericId);
        }
        
        // Remove the node
        objectNodes.Remove(numericId);
        return true;
    }

    public void SetParent(string childId, string? parentId)
    {
        var childNode = objectNodes[childId];

        BeatmapObjectNode? parentNode = null;
        if (parentId is not null)
            parentNode = objectNodes[parentId];

        var childNumericId = childNode.Object.Id.Numeric;
        
        // Remove from old parent
        if (childNode.Parent.HasValue)
        {
            if (objectNodes.TryGet(childNode.Parent.Value, out var oldParentNode))
                oldParentNode.Children.Remove(childNumericId);
        }
        
        // Set new parent
        if (parentNode is not null)
        {
            if (objectNodes.TryConvertStringIdToNumericId(parentId!, out var parentNumericId))
            {
                childNode.Parent = parentNumericId;
                parentNode.Children.Add(childNumericId);
            }
        }
        else
        {
            childNode.Parent = null;
        }
    }

    public bool TryGetParent(string id, out BeatmapObject? parent)
    {
        if (!objectNodes.TryGet(id, out var node))
        {
            parent = null;
            return false;
        }
        
        if (!node.Parent.HasValue)
        {
            parent = null;
            return true;
        }
        
        if (!objectNodes.TryGet(node.Parent.Value, out var parentNode))
        {
            parent = null;
            return false;
        }
        
        parent = parentNode.Object;
        return true;
    }

    public bool TryGetParent(int numericId, out BeatmapObject? parent)
    {
        if (!objectNodes.TryGet(numericId, out var node))
        {
            parent = null;
            return false;
        }
        
        if (!node.Parent.HasValue)
        {
            parent = null;
            return true;
        }
        
        if (!objectNodes.TryGet(node.Parent.Value, out var parentNode))
        {
            parent = null;
            return false;
        }
        
        parent = parentNode.Object;
        return true;
    }
    
    public bool TryGetChildren(string id, [MaybeNullWhen(false)] out List<BeatmapObject> children)
    {
        if (!objectNodes.TryGet(id, out var node))
        {
            children = null;
            return false;
        }
        
        children = node.Children
            .Select(childNumericId => !objectNodes.TryGet(childNumericId, out var childNode) ? null : childNode.Object)
            .OfType<BeatmapObject>()
            .ToList();
        return true;
    }
    
    public bool TryGetChildren(int numericId, [MaybeNullWhen(false)] out List<BeatmapObject> children)
    {
        if (!objectNodes.TryGet(numericId, out var node))
        {
            children = null;
            return false;
        }
        
        children = node.Children
            .Select(childNumericId => !objectNodes.TryGet(childNumericId, out var childNode) ? null : childNode.Object)
            .OfType<BeatmapObject>()
            .ToList();
        return true;
    }
    
    public bool TryGet(string id, [MaybeNullWhen(false)] out BeatmapObject beatmapObject)
    {
        if (!objectNodes.TryGet(id, out var node))
        {
            beatmapObject = null;
            return false;
        }
        
        beatmapObject = node.Object;
        return true;
    }
    
    public bool TryGet(int numericId, [MaybeNullWhen(false)] out BeatmapObject beatmapObject)
    {
        if (!objectNodes.TryGet(numericId, out var node))
        {
            beatmapObject = null;
            return false;
        }
        
        beatmapObject = node.Object;
        return true;
    }

    public bool Contains(string id) => objectNodes.Contains(id);
    
    public bool Contains(int numericId) => objectNodes.Contains(numericId);

    public IEnumerator<BeatmapObject> GetEnumerator()
        => objectNodes.Select(node => node.Object).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}