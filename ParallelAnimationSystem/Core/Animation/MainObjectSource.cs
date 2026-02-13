using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Model;
using ParallelAnimationSystem.Rendering.Data;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core.Animation;

public class MainObjectSource : IDisposable
{
    private const ulong PositionKey = 0;
    private const ulong ScaleKey = 1;
    private const ulong RotationKey = 2;

    private BeatmapData? attachedBeatmapData;

    private readonly TextRenderingService textRenderingService;
    private readonly PlaybackObjectContainer playbackObjects;
    private readonly RandomSeedService seedService;

    public MainObjectSource(TextRenderingService textRenderingService, PlaybackObjectContainer playbackObjects, RandomSeedService seedService)
    {
        this.textRenderingService = textRenderingService;
        this.playbackObjects = playbackObjects;
        this.seedService = seedService;
        
        this.seedService.SeedChanged += OnSeedChanged;
    }
    
    public void Dispose()
    {
        if (attachedBeatmapData is null)
            return;
        
        foreach (var beatmapObject in attachedBeatmapData.Objects.Values)
        {
            // remove from container
            var playbackObjectId = (Identifier)beatmapObject.Id;
            var playbackObjectIndex = playbackObjects.GetIndexForId(playbackObjectId);
            playbackObjects.Remove(playbackObjectIndex);
            playbackObjects.SetParent(playbackObjectIndex, null);
            
            // detach events
            beatmapObject.PropertyChanged -= OnBeatmapObjectPropertyChanged;
            beatmapObject.PositionKeyframesChanged -= OnBeatmapObjectPositionKeyframesChanged;
            beatmapObject.ScaleKeyframesChanged -= OnBeatmapObjectScaleKeyframesChanged;
            beatmapObject.RotationKeyframesChanged -= OnBeatmapObjectRotationKeyframesChanged;
            beatmapObject.ColorKeyframesChanged -= OnBeatmapObjectColorKeyframesChanged;
        }
        
        // detach events
        attachedBeatmapData.Objects.Inserted -= OnBeatmapObjectInserted;
        attachedBeatmapData.Objects.Removed -= OnBeatmapObjectRemoved;
        
        seedService.SeedChanged -= OnSeedChanged;
    }
    
    private void OnSeedChanged(object? sender, ulong e)
    {
        if (attachedBeatmapData is null)
            return;
        
        // update all playback objects' sequences
        foreach (var (id, beatmapObject) in attachedBeatmapData.Objects)
        {
            var playbackObjectId = (Identifier)id;
            var playbackObjectIndex = playbackObjects.GetIndexForId(playbackObjectId);
            if (!playbackObjects.TryGetItem(playbackObjectIndex, out var playbackObject))
                continue;
            
            var seed = NumberUtil.Mix(e, playbackObject.Id);
            
            playbackObject.PositionSequence.LoadKeyframes(KeyframeHelper.ResolveRandomizableVector2Keyframes(
                beatmapObject.PositionKeyframes, NumberUtil.Mix(seed, PositionKey)));
            
            playbackObject.ScaleSequence.LoadKeyframes(KeyframeHelper.ResolveRandomizableVector2Keyframes(
                beatmapObject.ScaleKeyframes, NumberUtil.Mix(seed, ScaleKey)));
            
            playbackObject.RotationSequence.LoadKeyframes(KeyframeHelper.ResolveRotationKeyframes(
                beatmapObject.RotationKeyframes, NumberUtil.Mix(seed, RotationKey)));
        }
    }

    public void AttachBeatmapData(BeatmapData beatmapData)
    {
        if (attachedBeatmapData is not null)
            throw new InvalidOperationException($"A {nameof(BeatmapData)} is already attached");
        
        attachedBeatmapData = beatmapData;
        
        // add existing objects
        foreach (var beatmapObject in beatmapData.Objects.Values)
            InsertBeatmapObject(beatmapObject);
        
        // attach events
        beatmapData.Objects.Inserted += OnBeatmapObjectInserted;
        beatmapData.Objects.Removed += OnBeatmapObjectRemoved;
    }
    
    public void DetachBeatmapData()
    {
        if (attachedBeatmapData is null)
            return;
        
        // remove existing objects
        foreach (var beatmapObject in attachedBeatmapData.Objects.Values)
        {
            // remove from container
            var playbackObjectId = (Identifier)beatmapObject.Id;
            var playbackObjectIndex = playbackObjects.GetIndexForId(playbackObjectId);
            playbackObjects.Remove(playbackObjectIndex);
            playbackObjects.SetParent(playbackObjectIndex, null);
            
            // detach events
            beatmapObject.PropertyChanged -= OnBeatmapObjectPropertyChanged;
            beatmapObject.PositionKeyframesChanged -= OnBeatmapObjectPositionKeyframesChanged;
            beatmapObject.ScaleKeyframesChanged -= OnBeatmapObjectScaleKeyframesChanged;
            beatmapObject.RotationKeyframesChanged -= OnBeatmapObjectRotationKeyframesChanged;
            beatmapObject.ColorKeyframesChanged -= OnBeatmapObjectColorKeyframesChanged;
        }
        
        // detach events
        attachedBeatmapData.Objects.Inserted -= OnBeatmapObjectInserted;
        attachedBeatmapData.Objects.Removed -= OnBeatmapObjectRemoved;
        
        attachedBeatmapData = null;
    }

    private void OnBeatmapObjectInserted(object? sender, BeatmapObject e)
    {
        InsertBeatmapObject(e);
    }

    private void OnBeatmapObjectRemoved(object? sender, BeatmapObject e)
    {
        RemoveBeatmapObject(e);
    }

    private void InsertBeatmapObject(BeatmapObject beatmapObject)
    {
        // create playback object
        var playbackObject = new PlaybackObject(beatmapObject.Id)
        {
            StartTime = beatmapObject.StartTime,
            EndTime = PlaybackObjectHelper.CalculateEndTime(
                beatmapObject.StartTime,
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
            Text = beatmapObject.Text is not null 
                ? textRenderingService.ShapeText(beatmapObject.Text, beatmapObject.Origin) 
                : null
        };
        
        // populate sequences
        var seed = NumberUtil.Mix(seedService.Seed, playbackObject.Id);
        playbackObject.PositionSequence.LoadKeyframes(KeyframeHelper.ResolveRandomizableVector2Keyframes(
            beatmapObject.PositionKeyframes, NumberUtil.Mix(seed, PositionKey)));
        
        playbackObject.ScaleSequence.LoadKeyframes(KeyframeHelper.ResolveRandomizableVector2Keyframes(
            beatmapObject.ScaleKeyframes, NumberUtil.Mix(seed, ScaleKey)));
        
        playbackObject.RotationSequence.LoadKeyframes(KeyframeHelper.ResolveRotationKeyframes(
            beatmapObject.RotationKeyframes, NumberUtil.Mix(seed, RotationKey)));
        
        playbackObject.ColorSequence.LoadKeyframes(PlaybackObjectHelper.ResolveColorKeyframes(beatmapObject.ColorKeyframes));
        
        // insert into container
        var index = playbackObjects.Insert(playbackObject);
        
        // set parent
        if (beatmapObject.ParentId is not null)
        {
            var parentIndex = playbackObjects.GetIndexForId(beatmapObject.ParentId);
            playbackObjects.SetParent(index, parentIndex);
        }
        else
        {
            playbackObjects.SetParent(index, null);
        }
        
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
        
        // detach events
        beatmapObject.PropertyChanged -= OnBeatmapObjectPropertyChanged;
        beatmapObject.PositionKeyframesChanged -= OnBeatmapObjectPositionKeyframesChanged;
        beatmapObject.ScaleKeyframesChanged -= OnBeatmapObjectScaleKeyframesChanged;
        beatmapObject.RotationKeyframesChanged -= OnBeatmapObjectRotationKeyframesChanged;
        beatmapObject.ColorKeyframesChanged -= OnBeatmapObjectColorKeyframesChanged;
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
                playbackObject.EndTime = PlaybackObjectHelper.CalculateEndTime(
                    beatmapObject.StartTime,
                    beatmapObject.Duration,
                    beatmapObject.AutoKillType,
                    beatmapObject.AutoKillOffset);
                break;
            case nameof(BeatmapObject.AutoKillType):
            case nameof(BeatmapObject.AutoKillOffset):
                playbackObject.EndTime = PlaybackObjectHelper.CalculateEndTime(
                    beatmapObject.StartTime,
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
                playbackObject.Text = beatmapObject.Text is not null 
                    ? textRenderingService.ShapeText(beatmapObject.Text, beatmapObject.Origin)
                    : null;
                break;
            case nameof(BeatmapObject.RenderDepth):
                playbackObject.RenderDepth = beatmapObject.RenderDepth;
                break;
            case nameof(BeatmapObject.Shape):
                playbackObject.Shape = beatmapObject.Shape;
                break;
            case nameof(BeatmapObject.Text):
                playbackObject.Text = beatmapObject.Text is not null 
                    ? textRenderingService.ShapeText(beatmapObject.Text, beatmapObject.Origin)
                    : null;
                break;
        }
    }

    private void OnBeatmapObjectPositionKeyframesChanged(object? sender, KeyframeList<Vector2Keyframe> e)
    {
        if (sender is not BeatmapObject beatmapObject)
            return;
        
        if (!TryGetPlaybackObject(beatmapObject, out var playbackObject, out _))
            return;
        
        var seed = NumberUtil.Mix(seedService.Seed, playbackObject.Id);
        playbackObject.PositionSequence.LoadKeyframes(
            KeyframeHelper.ResolveRandomizableVector2Keyframes(e, NumberUtil.Mix(seed, PositionKey)));
        
        // recalculate end time
        playbackObject.EndTime = PlaybackObjectHelper.CalculateEndTime(
            beatmapObject.StartTime,
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
        
        var seed = NumberUtil.Mix(seedService.Seed, playbackObject.Id);
        playbackObject.ScaleSequence.LoadKeyframes(
            KeyframeHelper.ResolveRandomizableVector2Keyframes(e, NumberUtil.Mix(seed, ScaleKey)));
        
        // recalculate end time
        playbackObject.EndTime = PlaybackObjectHelper.CalculateEndTime(
            beatmapObject.StartTime,
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
        
        var seed = NumberUtil.Mix(seedService.Seed, playbackObject.Id);
        playbackObject.RotationSequence.LoadKeyframes(
            KeyframeHelper.ResolveRotationKeyframes(e, NumberUtil.Mix(seed, RotationKey)));
        
        // recalculate end time
        playbackObject.EndTime = PlaybackObjectHelper.CalculateEndTime(
            beatmapObject.StartTime,
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
            beatmapObject.StartTime,
            beatmapObject.Duration,
            beatmapObject.AutoKillType,
            beatmapObject.AutoKillOffset);
    }
    
    private bool TryGetPlaybackObject(BeatmapObject beatmapObject, [MaybeNullWhen(false)] out PlaybackObject playbackObject, out int index)
    {
        index = playbackObjects.GetIndexForId(beatmapObject.Id);
        return playbackObjects.TryGetItem(index, out playbackObject);
    }
}