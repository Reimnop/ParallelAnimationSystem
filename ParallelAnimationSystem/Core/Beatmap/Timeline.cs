using System.ComponentModel;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core.Beatmap;

public class Timeline
{
   public float StartTimeOffset
   {
      get => startTimeOffset;
      set
      {
         if (startTimeOffset == value)
            return;
         
         startTimeOffset = value;
         killTimeDirty = true;
         startTimeDirty = true;
      }
   }

   public BeatmapObjectsContainer BeatmapObjects { get; } = new();
   public IReadOnlyCollection<BeatmapObject> AliveObjects => aliveObjects;
   
   private readonly HashSet<BeatmapObject> nonEmptyObjects = [];
   private readonly HashSet<BeatmapObject> aliveObjects = [];
   
   private readonly List<(float Time, BeatmapObject Object)> startTimeSortedObjects = [];
   private readonly List<(float Time, BeatmapObject Object)> killTimeSortedObjects = [];
   
   private bool startTimeDirty = true;
   private bool killTimeDirty = true;

   private int startIndex = 0;
   private int killIndex = 0;

   private float startTimeOffset = 0.0f;
   
   private float currentTime = 0.0f;

   public Timeline()
   {
      BeatmapObjects.BeatmapObjectAdded += OnBeatmapObjectAdded;
      BeatmapObjects.BeatmapObjectRemoved += OnBeatmapObjectRemoved;
   }

   private void OnBeatmapObjectAdded(object? sender, BeatmapObject beatmapObject)
   {
      if (!beatmapObject.IsEmpty)
      {
         // Flag start and kill times as dirty
         startTimeDirty = true;
         killTimeDirty = true;
         
         // Add to non-empty objects list
         nonEmptyObjects.Add(beatmapObject);
      }
      
      // Subscribe to property changed events
      beatmapObject.PropertyChanged += OnBeatmapObjectPropertyChanged;
   }

   private void OnBeatmapObjectRemoved(object? sender, BeatmapObject beatmapObject)
   {
      if (!beatmapObject.IsEmpty)
      {
         // Flag start and kill times as dirty
         startTimeDirty = true;
         killTimeDirty = true;
         
         // Remove from non-empty objects list
         nonEmptyObjects.Remove(beatmapObject);
      }
      
      // Unsubscribe from property changed events
      beatmapObject.PropertyChanged -= OnBeatmapObjectPropertyChanged;
   }

   public void ProcessFrame(float time)
   {
      // TODO: Maybe optimize this
      if (startTimeDirty)
      {
         startTimeSortedObjects.Clear();
         foreach (var obj in nonEmptyObjects)
            startTimeSortedObjects.Add((obj.StartTime + StartTimeOffset, obj));
         startTimeSortedObjects.Sort((a, b) => a.Time.CompareTo(b.Time));
         startIndex = CalculateIndex(time, startTimeSortedObjects, x => x.Time);
      }
      
      if (killTimeDirty)
      {
         killTimeSortedObjects.Clear();
         foreach (var obj in nonEmptyObjects)
            killTimeSortedObjects.Add((obj.CalculateKillTime(StartTimeOffset), obj));
         killTimeSortedObjects.Sort((a, b) => a.Time.CompareTo(b.Time));
         killIndex = CalculateIndex(time, killTimeSortedObjects, x => x.Time);
      }

      if (startTimeDirty || killTimeDirty)
      {
         // Recalculate alive objects
         aliveObjects.Clear();
         for (var i = 0; i < startIndex; i++)
            aliveObjects.Add(startTimeSortedObjects[i].Object);
         for (var i = 0; i < killIndex; i++)
            aliveObjects.Remove(killTimeSortedObjects[i].Object);
      }
      
      startTimeDirty = false;
      killTimeDirty = false;
      
      // Forward time direction
      if (time > currentTime)
      {
         while (startIndex < startTimeSortedObjects.Count && startTimeSortedObjects[startIndex].Time <= time)
         {
            aliveObjects.Add(startTimeSortedObjects[startIndex].Object);
            startIndex++;
         }
            
         while (killIndex < killTimeSortedObjects.Count && killTimeSortedObjects[killIndex].Time <= time)
         {
            aliveObjects.Remove(killTimeSortedObjects[killIndex].Object);
            killIndex++;
         }
      }
      // Reverse time direction
      else if (time < currentTime)
      {
         while (killIndex - 1 >= 0 && killTimeSortedObjects[killIndex - 1].Time > time)
         {
            aliveObjects.Add(killTimeSortedObjects[killIndex - 1].Object);
            killIndex--;
         }
            
         while (startIndex - 1 >= 0 && startTimeSortedObjects[startIndex - 1].Time > time)
         {
            aliveObjects.Remove(startTimeSortedObjects[startIndex - 1].Object);
            startIndex--;
         }
      }
      
      currentTime = time;
   }
   
   private static int CalculateIndex<T>(float time, List<T> list, Func<T, float> keySelector)
   {
      var index = list.BinarySearchKey(time, keySelector, Comparer<float>.Default);
      return index < 0 ? ~index : index + 1;
   }

   private void OnBeatmapObjectPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
   {
      if (eventArgs.PropertyName is nameof(BeatmapObject.IsEmpty))
      {
         // Check if object is empty or not
         if (sender is not BeatmapObject beatmapObject)
            return;
         
         // Flag start and kill times as dirty
         startTimeDirty = true;
         killTimeDirty = true;

         if (beatmapObject.IsEmpty) // State changed from non-empty to empty
            nonEmptyObjects.Remove(beatmapObject);
         else // State changed from empty to non-empty
            nonEmptyObjects.Add(beatmapObject);
      }
      
      if (eventArgs.PropertyName is nameof(BeatmapObject.StartTime))
      {
         startTimeDirty = true;
         killTimeDirty = true;
      }

      if (eventArgs.PropertyName is nameof(BeatmapObject.KillTimeOffset) or nameof(BeatmapObject.AutoKillType))
         killTimeDirty = true;
   }
}