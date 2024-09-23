using OpenTK.Mathematics;
using ParallelAnimationSystem.Core.Animation;

namespace ParallelAnimationSystem.Core;

public record ParentTransform(
    float TimeOffset,
    Sequence<Vector2, Vector2> PositionAnimation,
    Sequence<Vector2, Vector2> ScaleAnimation,
    Sequence<float, float> RotationAnimation,
    float ParentPositionTimeOffset,
    float ParentScaleTimeOffset,
    float ParentRotationTimeOffset,
    bool ParentAnimatePosition,
    bool ParentAnimateScale,
    bool ParentAnimateRotation);