using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Model;
using ParallelAnimationSystem.Rendering.Data;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core.Animation;

// do NOT touch parent handling
// you'll regret it
public class PrefabInstanceObjectSource(Identifier instanceId, PlaybackObjectContainer playbackObjects) : IDisposable
{
    private const ulong PositionKey = 0;
    private const ulong ScaleKey = 1;
    private const ulong RotationKey = 2;

    public BeatmapPrefab? AttachedPrefab => attachedPrefab;
    
    public float StartTime
    {
        get;
        set
        {
            if (field == value)
                return;
            
            field = value;
            UpdateStartTimes();
        }
    }

    public Vector2 Position
    {
        get;
        set
        {
            if (field == value)
                return;
            
            field = value;
            UpdateIntermediateParentTransforms();
        }
    }
    
    public Vector2 Scale
    {
        get;
        set
        {
            if (field == value)
                return;
            
            field = value;
            UpdateIntermediateParentTransforms();
        }
    } = Vector2.One;
    
    public float Rotation
    {
        get;
        set
        {
            if (field == value)
                return;
            
            field = value;
            UpdateIntermediateParentTransforms();
        }
    } = 0f;

    private float offset = 0f;

    private float EffectiveStartTime => StartTime - offset;

    private readonly Dictionary<string, PlaybackObject> intermediateParentByParentId = [];

    private readonly Dictionary<string, string?> internalParentById = []; // maps ids to parents of internal objects
    private readonly Dictionary<string, HashSet<string>> childrenById = []; // maps ids to children of all parents
    
    private BeatmapPrefab? attachedPrefab; 
    
    public void Dispose()
    {
        if (attachedPrefab is null)
            return;
        
        // remove intermediate parents
        foreach (var intermediateParent in intermediateParentByParentId.Values)
        {
            var intermediateParentIndex = playbackObjects.GetIndexForId(intermediateParent.Id);
            playbackObjects.Remove(intermediateParentIndex);
            playbackObjects.SetParent(intermediateParentIndex, null);
        }
        
        foreach (var beatmapObject in attachedPrefab.Objects.Values)
        {
            // remove from container
            var playbackObjectIndex = playbackObjects.GetIndexForId(GetInternalObjectId(beatmapObject.Id));
            playbackObjects.Remove(playbackObjectIndex);
            playbackObjects.SetParent(playbackObjectIndex, null);
            
            // detach events
            beatmapObject.PropertyChanged -= OnBeatmapObjectPropertyChanged;
            beatmapObject.PositionKeyframesChanged -= OnBeatmapObjectPositionKeyframesChanged;
            beatmapObject.ScaleKeyframesChanged -= OnBeatmapObjectScaleKeyframesChanged;
            beatmapObject.RotationKeyframesChanged -= OnBeatmapObjectRotationKeyframesChanged;
            beatmapObject.ColorKeyframesChanged -= OnBeatmapObjectColorKeyframesChanged;
        }
    }
    
    public void AttachPrefab(BeatmapPrefab beatmapPrefab)
    {
        if (attachedPrefab is not null)
            throw new InvalidOperationException($"A {nameof(BeatmapPrefab)} is already attached");
        
        attachedPrefab = beatmapPrefab;
        
        // insert existing objects
        foreach (var beatmapObject in beatmapPrefab.Objects.Values)
            InsertBeatmapObject(beatmapPrefab, beatmapObject);
        
        // attach events
        beatmapPrefab.Objects.Inserted += OnBeatmapObjectInserted;
        beatmapPrefab.Objects.Removed += OnBeatmapObjectRemoved;
        beatmapPrefab.PropertyChanged += OnPrefabPropertyChanged;
    }
    
    public void DetachPrefab()
    {
        if (attachedPrefab is null)
            return;
        
        // detach events
        attachedPrefab.Objects.Inserted -= OnBeatmapObjectInserted;
        attachedPrefab.Objects.Removed -= OnBeatmapObjectRemoved;
        attachedPrefab.PropertyChanged -= OnPrefabPropertyChanged;
        
        // remove intermediate parents
        foreach (var intermediateParent in intermediateParentByParentId.Values)
        {
            var intermediateParentIndex = playbackObjects.GetIndexForId(intermediateParent.Id);
            playbackObjects.Remove(intermediateParentIndex);
            playbackObjects.SetParent(intermediateParentIndex, null);
        }
        
        foreach (var beatmapObject in attachedPrefab.Objects.Values)
        {
            // remove from container
            var playbackObjectIndex = playbackObjects.GetIndexForId(GetInternalObjectId(beatmapObject.Id));
            playbackObjects.Remove(playbackObjectIndex);
            playbackObjects.SetParent(playbackObjectIndex, null);
            
            // detach events
            beatmapObject.PropertyChanged -= OnBeatmapObjectPropertyChanged;
            beatmapObject.PositionKeyframesChanged -= OnBeatmapObjectPositionKeyframesChanged;
            beatmapObject.ScaleKeyframesChanged -= OnBeatmapObjectScaleKeyframesChanged;
            beatmapObject.RotationKeyframesChanged -= OnBeatmapObjectRotationKeyframesChanged;
            beatmapObject.ColorKeyframesChanged -= OnBeatmapObjectColorKeyframesChanged;
        }
        
        intermediateParentByParentId.Clear();
        internalParentById.Clear();
        childrenById.Clear();
        
        attachedPrefab = null;
    }

    private void UpdateIntermediateParentTransforms()
    {
        foreach (var intermediateParent in intermediateParentByParentId.Values)
        {
            intermediateParent.PositionSequence.LoadKeyframes([ new Keyframe<Vector2>(0f, Ease.Linear, Position) ]);
            intermediateParent.ScaleSequence.LoadKeyframes([ new Keyframe<Vector2>(0f, Ease.Linear, Scale) ]);
            intermediateParent.RotationSequence.LoadKeyframes([ new Keyframe<float>(0f, Ease.Linear, Rotation) ]);
        }
    }

    private void OnPrefabPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BeatmapPrefab.Offset))
        {
            Debug.Assert(attachedPrefab is not null);
            offset = attachedPrefab.Offset;
            UpdateStartTimes();
        }
    }

    private void OnBeatmapObjectInserted(object? sender, BeatmapObject e)
    {
        Debug.Assert(attachedPrefab is not null);
        InsertBeatmapObject(attachedPrefab, e);
    }

    private void OnBeatmapObjectRemoved(object? sender, BeatmapObject e)
    {
        RemoveBeatmapObject(e);
    }

    private void InsertBeatmapObject(BeatmapPrefab beatmapPrefab, BeatmapObject beatmapObject)
    {
        // create playback object
        var playbackObject = new PlaybackObject(GetInternalObjectId(beatmapObject.Id))
        {
            StartTime = beatmapObject.StartTime + EffectiveStartTime,
            EndTime = PlaybackObjectHelper.CalculateEndTime(
                beatmapObject.StartTime + EffectiveStartTime,
                beatmapObject.Duration,
                beatmapObject.AutoKillType,
                beatmapObject.AutoKillOffset),
            IsVisible = beatmapObject.Type != BeatmapObjectType.Empty,
            ParentType = beatmapObject.ParentType,
            ParentOffset = beatmapObject.ParentOffset,
            RenderMode = (RenderMode)beatmapObject.RenderType,
            Origin = beatmapObject.Origin,
            RenderDepth = beatmapObject.RenderDepth,
            Shape = beatmapObject.Shape,
            Text = beatmapObject.Text
        };

        // populate sequences
        playbackObject.PositionSequence.LoadKeyframes(KeyframeHelper.ResolveRandomizableVector2Keyframes(
            beatmapObject.PositionKeyframes, NumberUtil.Mix(playbackObject.Id, PositionKey)));

        playbackObject.ScaleSequence.LoadKeyframes(KeyframeHelper.ResolveRandomizableVector2Keyframes(
            beatmapObject.ScaleKeyframes, NumberUtil.Mix(playbackObject.Id, ScaleKey)));

        playbackObject.RotationSequence.LoadKeyframes(KeyframeHelper.ResolveRotationKeyframes(
            beatmapObject.RotationKeyframes, NumberUtil.Mix(playbackObject.Id, RotationKey)));

        playbackObject.ColorSequence.LoadKeyframes(PlaybackObjectHelper.ResolveColorKeyframes(beatmapObject.ColorKeyframes));
        
        // before inserting, check if we have any intermediate object of the same ID
        if (intermediateParentByParentId.TryGetValue(beatmapObject.Id, out var intermediateParent))
        {
            // we have an intermediate parent with the same ID as the beatmap object
            // we need to remove the intermediate parent and insert the beatmap object in its place
            
            var intermediateParentIndex = playbackObjects.GetIndexForId(intermediateParent.Id);
            playbackObjects.Remove(intermediateParentIndex);
            playbackObjects.SetParent(intermediateParentIndex, null);
            intermediateParentByParentId.Remove(beatmapObject.Id);
            
            // we don't need to reparent children, because both the intermediate parent and this object share the same internal ID
        }

        // insert into container
        var index = playbackObjects.Insert(playbackObject);

        // resolve parent
        if (beatmapObject.ParentId is not null) // case: we have a parent
        {
            // case: parent is in the prefab
            if (beatmapPrefab.Objects.ContainsKey(beatmapObject.ParentId))
            {
                var parentIndex = playbackObjects.GetIndexForId(GetInternalObjectId(beatmapObject.ParentId));
                playbackObjects.SetParent(index, parentIndex);
                
                var childrenSet = childrenById.GetOrInsert(beatmapObject.ParentId, () => []);
                childrenSet.Add(beatmapObject.Id);
            }
            else // case: parent is external
            {
                // get intermediate parent object (create if it doesn't exist)
                GetIntermediateParent(beatmapObject.ParentId, out var intermediateParentIndex);

                // parent this playback object to the intermediate parent
                playbackObjects.SetParent(index, intermediateParentIndex);
                
                var childrenSet = childrenById.GetOrInsert(beatmapObject.ParentId, () => []);
                childrenSet.Add(beatmapObject.Id);
            }
        }
        else // case: we don't have a parent
        {
            // get intermediate parent object (create if it doesn't exist)
            GetIntermediateParent(string.Empty, out var intermediateParentIndex);

            // parent this playback object to the intermediate parent
            playbackObjects.SetParent(index, intermediateParentIndex);
            
            var childrenSet = childrenById.GetOrInsert(string.Empty, () => []);
            childrenSet.Add(beatmapObject.Id);
        }
        
        internalParentById.Add(beatmapObject.Id, beatmapObject.ParentId);

        // attach events
        beatmapObject.PropertyChanged += OnBeatmapObjectPropertyChanged;
        beatmapObject.PositionKeyframesChanged += OnBeatmapObjectPositionKeyframesChanged;
        beatmapObject.ScaleKeyframesChanged += OnBeatmapObjectScaleKeyframesChanged;
        beatmapObject.RotationKeyframesChanged += OnBeatmapObjectRotationKeyframesChanged;
        beatmapObject.ColorKeyframesChanged += OnBeatmapObjectColorKeyframesChanged;
    }

    private void RemoveBeatmapObject(BeatmapObject beatmapObject)
    {
        // remove from container
        var playbackObjectIndex = playbackObjects.GetIndexForId(beatmapObject.Id);
        playbackObjects.Remove(playbackObjectIndex);
        internalParentById.Remove(beatmapObject.Id);
        
        // remove this object from its parent's children set
        if (beatmapObject.ParentId is not null)
        {
            if (childrenById.TryGetValue(beatmapObject.ParentId, out var childrenSet1))
            {
                childrenSet1.Remove(beatmapObject.Id);
                if (childrenSet1.Count == 0)
                {
                    childrenById.Remove(beatmapObject.ParentId);
                    
                    // if the parent was also an intermediate parent, and there are no more children, remove it
                    if (intermediateParentByParentId.TryGetValue(beatmapObject.ParentId, out var intermediateParent))
                    {
                        var intermediateParentIndex = playbackObjects.GetIndexForId(intermediateParent.Id);
                        playbackObjects.Remove(intermediateParentIndex);
                        playbackObjects.SetParent(intermediateParentIndex, null);
                        intermediateParentByParentId.Remove(beatmapObject.ParentId);
                    }
                }
            }
        }
        
        // ensure there is an intermediate parent in this one's place
        // if there are any children
        if (childrenById.TryGetValue(beatmapObject.Id, out var childrenSet2) && childrenSet2.Count > 0)
            GetIntermediateParent(beatmapObject.Id, out _);
        
        // detach events
        beatmapObject.PropertyChanged -= OnBeatmapObjectPropertyChanged;
        beatmapObject.PositionKeyframesChanged -= OnBeatmapObjectPositionKeyframesChanged;
        beatmapObject.ScaleKeyframesChanged -= OnBeatmapObjectScaleKeyframesChanged;
        beatmapObject.RotationKeyframesChanged -= OnBeatmapObjectRotationKeyframesChanged;
        beatmapObject.ColorKeyframesChanged -= OnBeatmapObjectColorKeyframesChanged;
    }

    private PlaybackObject GetIntermediateParent(string parentId, out int index)
    {
        // check if we already have an intermediate parent for this ID
        if (intermediateParentByParentId.TryGetValue(parentId, out var playbackObject))
        {
            index = playbackObjects.GetIndexForId(playbackObject.Id);
            return playbackObject;
        }

        // if we don't, create new intermediate parent
        var internalId = GetInternalObjectId(parentId);
        playbackObject = new PlaybackObject(internalId)
        {
            StartTime = 0f,
            EndTime = float.MaxValue,
            ParentType = ParentType.All
        };
        
        playbackObject.PositionSequence.LoadKeyframes([ new Keyframe<Vector2>(0f, Ease.Linear, Position) ]);
        playbackObject.ScaleSequence.LoadKeyframes([ new Keyframe<Vector2>(0f, Ease.Linear, Scale) ]);
        playbackObject.RotationSequence.LoadKeyframes([ new Keyframe<float>(0f, Ease.Linear, Rotation) ]);
        
        index = playbackObjects.Insert(playbackObject);
        
        // parent this intermediate parent to the parent ID
        if (parentId != string.Empty)
        {
            // since we're getting the EXTERNAl parent, we don't use GetInternalObjectId here
            var parentIndex = playbackObjects.GetIndexForId(parentId);
            playbackObjects.SetParent(index, parentIndex);
        }
        
        intermediateParentByParentId.Add(parentId, playbackObject);
        return playbackObject;
    }
    
    private void UpdateStartTimes()
    {
        if (attachedPrefab is null)
            return;
        
        foreach (var beatmapObject in attachedPrefab.Objects.Values)
        {
            if (!TryGetPlaybackObject(beatmapObject, out var playbackObject, out _))
                continue;
            
            playbackObject.StartTime = beatmapObject.StartTime + EffectiveStartTime;
            playbackObject.EndTime = PlaybackObjectHelper.CalculateEndTime(
                beatmapObject.StartTime + EffectiveStartTime,
                beatmapObject.Duration,
                beatmapObject.AutoKillType,
                beatmapObject.AutoKillOffset);
        }
    }

    private void OnBeatmapObjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not BeatmapObject beatmapObject)
            return;
        
        if (!TryGetPlaybackObject(beatmapObject, out var playbackObject, out var index))
            return;

        switch (e.PropertyName)
        {
            case nameof(BeatmapObject.ParentId):
            {
                // get old parent
                if (internalParentById.TryGetValue(beatmapObject.Id, out var internalParent))
                {
                    // remove from old parent's children set
                    if (internalParent is not null)
                    {
                        if (childrenById.TryGetValue(internalParent, out var oldChildrenSet))
                        {
                            oldChildrenSet.Remove(beatmapObject.Id);
                            if (oldChildrenSet.Count == 0)
                            {
                                childrenById.Remove(internalParent);
                                
                                // if the old parent was also an intermediate parent, and there are no more children, remove it
                                if (intermediateParentByParentId.TryGetValue(internalParent, out var intermediateParent))
                                {
                                    var intermediateParentIndex = playbackObjects.GetIndexForId(intermediateParent.Id);
                                    playbackObjects.Remove(intermediateParentIndex);
                                    playbackObjects.SetParent(intermediateParentIndex, null);
                                    intermediateParentByParentId.Remove(internalParent);
                                }
                            }
                        }
                    }
                }
                
                // set new parent
                Debug.Assert(attachedPrefab is not null);
                if (beatmapObject.ParentId is not null) // case: we have a parent
                {
                    // case: parent is in the prefab
                    if (attachedPrefab.Objects.ContainsKey(beatmapObject.ParentId))
                    {
                        var parentIndex = playbackObjects.GetIndexForId(GetInternalObjectId(beatmapObject.ParentId));
                        playbackObjects.SetParent(index, parentIndex);
                        
                        var childrenSet = childrenById.GetOrInsert(beatmapObject.ParentId, () => []);
                        childrenSet.Add(beatmapObject.Id);
                    }
                    else // case: parent is external
                    {
                        // get intermediate parent object (create if it doesn't exist)
                        GetIntermediateParent(beatmapObject.ParentId, out var intermediateParentIndex);

                        // parent this playback object to the intermediate parent
                        playbackObjects.SetParent(index, intermediateParentIndex);
                        
                        var childrenSet = childrenById.GetOrInsert(beatmapObject.ParentId, () => []);
                        childrenSet.Add(beatmapObject.Id);
                    }
                }
                else // case: we don't have a parent
                {
                    // get intermediate parent object (create if it doesn't exist)
                    GetIntermediateParent(string.Empty, out var intermediateParentIndex);

                    // parent this playback object to the intermediate parent
                    playbackObjects.SetParent(index, intermediateParentIndex);
                    
                    var childrenSet = childrenById.GetOrInsert(string.Empty, () => []);
                    childrenSet.Add(beatmapObject.Id);
                }
                
                internalParentById[beatmapObject.Id] = beatmapObject.ParentId;
                break;
            }
            case nameof(BeatmapObject.StartTime):
                playbackObject.StartTime = beatmapObject.StartTime + EffectiveStartTime;
                playbackObject.EndTime = PlaybackObjectHelper.CalculateEndTime(
                    beatmapObject.StartTime + EffectiveStartTime,
                    beatmapObject.Duration,
                    beatmapObject.AutoKillType,
                    beatmapObject.AutoKillOffset);
                break;
            case nameof(BeatmapObject.AutoKillType):
            case nameof(BeatmapObject.AutoKillOffset):
                playbackObject.EndTime = PlaybackObjectHelper.CalculateEndTime(
                    beatmapObject.StartTime + EffectiveStartTime,
                    beatmapObject.Duration,
                    beatmapObject.AutoKillType,
                    beatmapObject.AutoKillOffset);
                break;
            case nameof(BeatmapObject.Type):
                playbackObject.IsVisible = beatmapObject.Type != BeatmapObjectType.Empty;
                break;
            case nameof(BeatmapObject.ParentType):
                playbackObject.ParentType = beatmapObject.ParentType;
                break;
            case nameof(BeatmapObject.ParentOffset):
                playbackObject.ParentOffset = beatmapObject.ParentOffset;
                break;
            case nameof(BeatmapObject.RenderType):
                playbackObject.RenderMode = (RenderMode)beatmapObject.RenderType;
                break;
            case nameof(BeatmapObject.Origin):
                playbackObject.Origin = beatmapObject.Origin;
                break;
            case nameof(BeatmapObject.RenderDepth):
                playbackObject.RenderDepth = beatmapObject.RenderDepth;
                break;
            case nameof(BeatmapObject.Shape):
                playbackObject.Shape = beatmapObject.Shape;
                break;
            case nameof(BeatmapObject.Text):
                playbackObject.Text = beatmapObject.Text;
                break;
        }
    }

    private void OnBeatmapObjectPositionKeyframesChanged(object? sender, KeyframeList<Vector2Keyframe> e)
    {
        if (sender is not BeatmapObject beatmapObject)
            return;
        
        if (!TryGetPlaybackObject(beatmapObject, out var playbackObject, out _))
            return;
        
        playbackObject.PositionSequence.LoadKeyframes(KeyframeHelper.ResolveRandomizableVector2Keyframes(
            e, NumberUtil.Mix(playbackObject.Id, PositionKey)));
        
        // recalculate end time
        playbackObject.EndTime = PlaybackObjectHelper.CalculateEndTime(
            beatmapObject.StartTime + EffectiveStartTime,
            beatmapObject.Duration,
            beatmapObject.AutoKillType,
            beatmapObject.AutoKillOffset);
    }

    private void OnBeatmapObjectScaleKeyframesChanged(object? sender, KeyframeList<Vector2Keyframe> e)
    {
        if (sender is not BeatmapObject beatmapObject)
            return;
        
        if (!TryGetPlaybackObject(beatmapObject, out var playbackObject, out _))
            return;
        
        playbackObject.ScaleSequence.LoadKeyframes(KeyframeHelper.ResolveRandomizableVector2Keyframes(
            e, NumberUtil.Mix(playbackObject.Id, ScaleKey)));
        
        // recalculate end time
        playbackObject.EndTime = PlaybackObjectHelper.CalculateEndTime(
            beatmapObject.StartTime + EffectiveStartTime,
            beatmapObject.Duration,
            beatmapObject.AutoKillType,
            beatmapObject.AutoKillOffset);
    }

    private void OnBeatmapObjectRotationKeyframesChanged(object? sender, KeyframeList<RotationKeyframe> e)
    {
        if (sender is not BeatmapObject beatmapObject)
            return;
        
        if (!TryGetPlaybackObject(beatmapObject, out var playbackObject, out _))
            return;
        
        playbackObject.RotationSequence.LoadKeyframes(KeyframeHelper.ResolveRotationKeyframes(
            e, NumberUtil.Mix(playbackObject.Id, RotationKey)));
        
        // recalculate end time
        playbackObject.EndTime = PlaybackObjectHelper.CalculateEndTime(
            beatmapObject.StartTime + EffectiveStartTime,
            beatmapObject.Duration,
            beatmapObject.AutoKillType,
            beatmapObject.AutoKillOffset);
    }

    private void OnBeatmapObjectColorKeyframesChanged(object? sender, KeyframeList<BeatmapObjectColorKeyframe> e)
    {
        if (sender is not BeatmapObject beatmapObject)
            return;
        
        if (!TryGetPlaybackObject(beatmapObject, out var playbackObject, out _))
            return;
        
        playbackObject.ColorSequence.LoadKeyframes(PlaybackObjectHelper.ResolveColorKeyframes(e));
        
        // recalculate end time
        playbackObject.EndTime = PlaybackObjectHelper.CalculateEndTime(
            beatmapObject.StartTime + EffectiveStartTime,
            beatmapObject.Duration,
            beatmapObject.AutoKillType,
            beatmapObject.AutoKillOffset);
    }
    
    private bool TryGetPlaybackObject(BeatmapObject beatmapObject, [MaybeNullWhen(false)] out PlaybackObject playbackObject, out int index)
    {
        index = playbackObjects.GetIndexForId(GetInternalObjectId(beatmapObject.Id));
        return playbackObjects.TryGetItem(index, out playbackObject);
    }

    private Identifier GetInternalObjectId(string id)
        => Identifier.Combine(instanceId, id);
}