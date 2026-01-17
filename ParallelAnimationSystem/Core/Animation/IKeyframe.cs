using Pamx.Common.Enum;

namespace ParallelAnimationSystem.Core.Animation;

/// <summary>
/// Interface for a keyframe in an animation
/// </summary>
public interface IKeyframe
{
    /// <summary>
    /// The time of the keyframe in seconds
    /// </summary>
    public float Time { get; }
    
    /// <summary>
    /// The easing function used for interpolation to the next keyframe
    /// </summary>
    public Ease Ease { get; }
}