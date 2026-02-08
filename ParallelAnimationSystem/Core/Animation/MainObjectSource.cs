using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Model;
using ParallelAnimationSystem.Rendering.Data;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core.Animation;

public class MainObjectSource(PlaybackObjectContainer playbackObjects) : IDisposable
{
    private const ulong PositionKey = 0;
    private const ulong ScaleKey = 1;
    private const ulong RotationKey = 2;
    
    private readonly Dictionary<string, BeatmapObject> beatmapObjects = [];
    
    public void Dispose()
    {
        foreach (var beatmapObject in beatmapObjects.Values)
        {
            // remove from container
            var playbackObjectId = (Identifier)beatmapObject.Id;
            var playbackObjectIndex = playbackObjects.GetIndexForId(playbackObjectId);
            playbackObjects.Remove(playbackObjectIndex);
            
            // detach events
            beatmapObject.PropertyChanged -= OnBeatmapObjectPropertyChanged;
            beatmapObject.PositionKeyframesChanged -= OnBeatmapObjectPositionKeyframesChanged;
            beatmapObject.ScaleKeyframesChanged -= OnBeatmapObjectScaleKeyframesChanged;
            beatmapObject.RotationKeyframesChanged -= OnBeatmapObjectRotationKeyframesChanged;
            beatmapObject.ColorKeyframesChanged -= OnBeatmapObjectColorKeyframesChanged;
        }
    }
    
    public bool InsertBeatmapObject(BeatmapObject beatmapObject)
    {
        if (!beatmapObjects.TryAdd(beatmapObject.Id, beatmapObject))
            return false;
        
        // create playback object
        var playbackObject = new PlaybackObject(beatmapObject.Id)
        {
            StartTime = beatmapObject.StartTime,
            EndTime = CalculateEndTime(beatmapObject),
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
        
        playbackObject.ColorSequence.LoadKeyframes(ResolveColorKeyframes(beatmapObject.ColorKeyframes));
        
        // insert into container
        var index = playbackObjects.Insert(playbackObject);
        
        // set parent
        if (beatmapObject.ParentId is not null)
        {
            var parentIndex = playbackObjects.GetIndexForId(beatmapObject.ParentId);
            playbackObjects.SetParent(index, parentIndex);
        }
        
        // attach events
        beatmapObject.PropertyChanged += OnBeatmapObjectPropertyChanged;
        beatmapObject.PositionKeyframesChanged += OnBeatmapObjectPositionKeyframesChanged;
        beatmapObject.ScaleKeyframesChanged += OnBeatmapObjectScaleKeyframesChanged;
        beatmapObject.RotationKeyframesChanged += OnBeatmapObjectRotationKeyframesChanged;
        beatmapObject.ColorKeyframesChanged += OnBeatmapObjectColorKeyframesChanged;
        
        return true;
    }

    public bool RemoveBeatmapObject(BeatmapObject beatmapObject)
    {
        if (!beatmapObjects.Remove(beatmapObject.Id))
            return false;
        
        // remove from container
        var playbackObjectId = (Identifier)beatmapObject.Id;
        var playbackObjectIndex = playbackObjects.GetIndexForId(playbackObjectId);
        playbackObjects.Remove(playbackObjectIndex);
        
        // detach events
        beatmapObject.PropertyChanged -= OnBeatmapObjectPropertyChanged;
        beatmapObject.PositionKeyframesChanged -= OnBeatmapObjectPositionKeyframesChanged;
        beatmapObject.ScaleKeyframesChanged -= OnBeatmapObjectScaleKeyframesChanged;
        beatmapObject.RotationKeyframesChanged -= OnBeatmapObjectRotationKeyframesChanged;
        beatmapObject.ColorKeyframesChanged -= OnBeatmapObjectColorKeyframesChanged;
        
        return true;
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
                if (beatmapObject.ParentId is not null)
                {
                    var parentIndex = playbackObjects.GetIndexForId(beatmapObject.ParentId);
                    playbackObjects.SetParent(index, parentIndex);
                }
                else
                {
                    playbackObjects.SetParent(index, null);
                }
                break;
            case nameof(BeatmapObject.StartTime):
                playbackObject.StartTime = beatmapObject.StartTime;
                playbackObject.EndTime = CalculateEndTime(beatmapObject);
                break;
            case nameof(BeatmapObject.AutoKillType):
            case nameof(BeatmapObject.AutoKillOffset):
                playbackObject.EndTime = CalculateEndTime(beatmapObject);
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
        playbackObject.EndTime = CalculateEndTime(beatmapObject);
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
        playbackObject.EndTime = CalculateEndTime(beatmapObject);
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
        playbackObject.EndTime = CalculateEndTime(beatmapObject);
    }

    private void OnBeatmapObjectColorKeyframesChanged(object? sender, KeyframeList<BeatmapObjectColorKeyframe> e)
    {
        if (sender is not BeatmapObject beatmapObject)
            return;
        
        if (!TryGetPlaybackObject(beatmapObject, out var playbackObject, out _))
            return;
        
        playbackObject.ColorSequence.LoadKeyframes(ResolveColorKeyframes(e));
        
        // recalculate end time
        playbackObject.EndTime = CalculateEndTime(beatmapObject);
    }
    
    private bool TryGetPlaybackObject(BeatmapObject beatmapObject, [MaybeNullWhen(false)] out PlaybackObject playbackObject, out int index)
    {
        var id = (Identifier)beatmapObject.Id;
        index = playbackObjects.GetIndexForId(id);
        return playbackObjects.TryGetItem(index, out playbackObject);
    }

    private static float CalculateEndTime(BeatmapObject beatmapObject)
        => beatmapObject.AutoKillType switch
        {
            AutoKillType.NoAutoKill => float.PositiveInfinity,
            AutoKillType.LastKeyframe => beatmapObject.StartTime + beatmapObject.Duration,
            AutoKillType.LastKeyframeOffset => beatmapObject.StartTime + beatmapObject.Duration + beatmapObject.AutoKillOffset,
            AutoKillType.FixedTime => beatmapObject.StartTime + beatmapObject.AutoKillOffset,
            AutoKillType.SongTime => beatmapObject.AutoKillOffset,
            _ => float.PositiveInfinity // no auto kill by default
        };
    
    private static IEnumerable<Keyframe<BeatmapObjectIndexedColor>> ResolveColorKeyframes(IEnumerable<BeatmapObjectColorKeyframe> keyframes)
    {
        foreach (var kf in keyframes)
        {
            var value = new BeatmapObjectIndexedColor
            {
                ColorIndex1 = kf.Color.ColorIndex1,
                ColorIndex2 = kf.Color.ColorIndex2,
                Opacity = kf.Color.Opacity
            };
            
            yield return new Keyframe<BeatmapObjectIndexedColor>(kf.Time, kf.Ease, value);
        }
    }
}