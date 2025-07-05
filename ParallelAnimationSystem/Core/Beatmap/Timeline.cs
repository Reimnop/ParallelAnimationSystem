using System.ComponentModel;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core.Beatmap;

public class Timeline
{
   public event EventHandler<BeatmapObject>? BeatmapObjectAdded;
   public event EventHandler<BeatmapObject>? BeatmapObjectRemoved;
   
   public BeatmapObject RootObject { get; } = new(
      string.Empty,
      new BeatmapObjectData([], [], [], []));

   public IReadOnlyCollection<BeatmapObject> AllObjects => allObjects;
   public IReadOnlyCollection<BeatmapObject> AliveObjects => aliveObjects;

   private readonly HashSet<BeatmapObject> allObjects = [];
   private readonly HashSet<BeatmapObject> aliveObjects = [];
   
   private readonly List<(float Time, BeatmapObject Object)> startTimeSortedObjects = [];
   private readonly List<(float Time, BeatmapObject Object)> killTimeSortedObjects = [];
   
   private bool startTimeDirty = true;
   private bool killTimeDirty = true;

   private int startIndex = 0;
   private int killIndex = 0;
   
   private float currentTime = 0.0f;

   public Timeline()
   {
      RootObject.ChildAdded += OnBeatmapObjectChildAdded;
      RootObject.ChildRemoved += OnBeatmapObjectChildRemoved;
   }

   private void OnBeatmapObjectChildAdded(object? sender, BeatmapObject beatmapObject)
   {
      // Add object and all its children to allObjects
      beatmapObject.Traverse(obj =>
      {
         obj.ChildAdded += OnBeatmapObjectChildAdded;
         obj.ChildRemoved += OnBeatmapObjectChildRemoved;
         obj.Data.PropertyChanged += OnBeatmapObjectDataPropertyChanged;
         
         allObjects.Add(obj);
      });
      
      // Flag start and kill times as dirty
      startTimeDirty = true;
      killTimeDirty = true;
      
      // Call event handler
      beatmapObject.Traverse(obj => BeatmapObjectAdded?.Invoke(this, obj));
   }

   private void OnBeatmapObjectChildRemoved(object? sender, BeatmapObject beatmapObject)
   {
      // Remove object and all its children from allObjects
      beatmapObject.Traverse(obj =>
      {
         obj.ChildAdded -= OnBeatmapObjectChildAdded;
         obj.ChildRemoved -= OnBeatmapObjectChildRemoved;
         obj.Data.PropertyChanged -= OnBeatmapObjectDataPropertyChanged;
         
         allObjects.Remove(obj);
      });
      
      // Flag start and kill times as dirty
      startTimeDirty = true;
      killTimeDirty = true;
      
      // Call event handler
      beatmapObject.Traverse(obj => BeatmapObjectRemoved?.Invoke(this, obj));
   }

   public void ProcessFrame(float time)
   {
      if (startTimeDirty)
      {
         startTimeSortedObjects.Clear();
         foreach (var obj in allObjects)
            startTimeSortedObjects.Add((obj.Data.StartTime, obj));
         startTimeSortedObjects.Sort((a, b) => a.Time.CompareTo(b.Time));
         startIndex = CalculateIndex(time, startTimeSortedObjects, x => x.Time);
      }
      
      if (killTimeDirty)
      {
         killTimeSortedObjects.Clear();
         foreach (var obj in allObjects)
            killTimeSortedObjects.Add((CalculateKillTime(obj.Data), obj));
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

   private float CalculateKillTime(BeatmapObjectData data)
      => data.AutoKillType switch
      {
         AutoKillType.FixedTime => data.StartTime + data.KillTimeOffset,
         AutoKillType.NoAutoKill => float.PositiveInfinity,
         AutoKillType.LastKeyframe => throw new NotImplementedException(),
         AutoKillType.LastKeyframeOffset => throw new NotImplementedException(),
         AutoKillType.SongTime => data.KillTimeOffset,
         _ => throw new ArgumentOutOfRangeException(nameof(data.AutoKillType), $"Unknown AutoKillType '{data.AutoKillType}'!")
      };

   private void OnBeatmapObjectDataPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
   {
      if (eventArgs.PropertyName is nameof(BeatmapObjectData.StartTime))
      {
         startTimeDirty = true;
         killTimeDirty = true;
      }

      if (eventArgs.PropertyName is nameof(BeatmapObjectData.KillTimeOffset) or nameof(BeatmapObjectData.AutoKillType))
         killTimeDirty = true;
   }
}