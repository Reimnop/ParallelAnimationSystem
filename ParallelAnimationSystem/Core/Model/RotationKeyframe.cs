using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Model;

public struct RotationKeyframe : IKeyframe
{
    public required float Time { get; init; }
    public required Ease Ease { get; init; }
    public required float Value { get; init; }
    public required RandomMode RandomMode { get; init; }
    public required float RandomValue { get; init; }
    public required float RandomInterval { get; init; }
    public required bool IsRelative { get; init; }
}