using System.Numerics;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Model;

public struct Vector2Keyframe : IKeyframe
{
    public required float Time { get; init; }
    public required Ease Ease { get; init; }
    public required Vector2 Value { get; init; }
    public required RandomMode RandomMode { get; init; }
    public required Vector2 RandomValue { get; init; }
    public required float RandomInterval { get; init; }
}