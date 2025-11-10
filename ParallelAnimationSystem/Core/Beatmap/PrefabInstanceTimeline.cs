using System.ComponentModel;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core.Beatmap;

public class PrefabInstanceTimeline : IDisposable
{
   public event EventHandler<PrefabInstanceObject>? PrefabInstanceObjectAdded;
   public event EventHandler<PrefabInstanceObject>? PrefabInstanceObjectRemoved;

   public IReadOnlyCollection<PrefabInstanceObject> AllObjects => allObjects;
   public IReadOnlyCollection<PrefabInstanceObject> AliveObjects => aliveObjects;

   private readonly HashSet<PrefabInstanceObject> allObjects = [];
   private readonly HashSet<PrefabInstanceObject> aliveObjects = [];
   
   private readonly List<(float Time, PrefabInstanceObject Object)> startTimeSortedObjects = [];
   private readonly List<(float Time, PrefabInstanceObject Object)> killTimeSortedObjects = [];
   
   private bool startTimeDirty = true;
   private bool killTimeDirty = true;

   private int startIndex = 0;
   private int killIndex = 0;
   
   private float currentTime = 0.0f;
   
   ~PrefabInstanceTimeline()
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
          Console.Error.WriteLine("PrefabInstanceTimeline finalizer called without disposing. Did you forget to call Dispose()?");
      
      // Unsubscribe from events
      foreach (var obj in allObjects)
      {
         obj.StartTimeChanged -= OnPrefabInstanceObjectStartTimeChanged;
         obj.KillTimeChanged -= OnPrefabInstanceObjectKillTimeChanged;
      }
   }

   public bool Add(PrefabInstanceObject prefabInstanceObject)
   {
      if (!allObjects.Add(prefabInstanceObject)) 
         return false;
      
      // Subscribe to events
      prefabInstanceObject.StartTimeChanged += OnPrefabInstanceObjectStartTimeChanged;
      prefabInstanceObject.KillTimeChanged += OnPrefabInstanceObjectKillTimeChanged;
      
      startTimeDirty = true;
      killTimeDirty = true;
      
      PrefabInstanceObjectAdded?.Invoke(this, prefabInstanceObject);
      
      return true;
   }

   public bool Remove(PrefabInstanceObject prefabInstanceObject)
   {
      if (!allObjects.Remove(prefabInstanceObject)) 
         return false;
      
      // Unsubscribe from events
      prefabInstanceObject.StartTimeChanged -= OnPrefabInstanceObjectStartTimeChanged;
      prefabInstanceObject.KillTimeChanged -= OnPrefabInstanceObjectKillTimeChanged;
      
      startTimeDirty = true;
      killTimeDirty = true;
      
      PrefabInstanceObjectRemoved?.Invoke(this, prefabInstanceObject);
      
      return true;
   }
   
   private void OnPrefabInstanceObjectStartTimeChanged(object? sender, PrefabInstanceObject e)
   {
      startTimeDirty = true;
   }

   private void OnPrefabInstanceObjectKillTimeChanged(object? sender, PrefabInstanceObject e)
   {
      killTimeDirty = true;
   }

   public void ProcessFrame(float time)
   {
      // TODO: Maybe optimize this
      if (startTimeDirty)
      {
         startTimeSortedObjects.Clear();
         foreach (var obj in allObjects)
            startTimeSortedObjects.Add((obj.StartTime, obj));
         startTimeSortedObjects.Sort((a, b) => a.Time.CompareTo(b.Time));
         startIndex = CalculateIndex(time, startTimeSortedObjects, x => x.Time);
      }
      
      if (killTimeDirty)
      {
         killTimeSortedObjects.Clear();
         foreach (var obj in allObjects)
            killTimeSortedObjects.Add((obj.KillTime, obj));
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
}