namespace ParallelAnimationSystem.Core.Beatmap;

public class Timeline
{
   public event EventHandler<BeatmapObject>? RootObjectAdded;
   public event EventHandler<BeatmapObject>? RootObjectRemoved;
   
   public IReadOnlyCollection<BeatmapObject> RootObjects => rootObjects;

   private readonly List<BeatmapObject> rootObjects = [];

   public void AddRootObject(BeatmapObject beatmapObject)
   {
      if (beatmapObject.Parent != null)
         throw new ArgumentException("Cannot add an object that already has a parent as a root object.", nameof(beatmapObject));
      
      rootObjects.Add(beatmapObject);
      
      RootObjectAdded?.Invoke(this, beatmapObject);
   }
   
   public void RemoveRootObject(BeatmapObject beatmapObject)
   {
      if (!rootObjects.Remove(beatmapObject))
         throw new ArgumentException("The specified object is not a root object in this timeline.", nameof(beatmapObject));
      
      RootObjectRemoved?.Invoke(this, beatmapObject);
   }
}