using System.Numerics;

namespace ParallelAnimationSystem.Core.Data;

public interface IEventData
{
    Vector2 CameraPosition { get; }
    float CameraScale { get; }
    float CameraRotation { get; }
}