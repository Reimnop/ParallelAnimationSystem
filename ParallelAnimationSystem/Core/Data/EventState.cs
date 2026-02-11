using System.Numerics;

namespace ParallelAnimationSystem.Core.Data;

public class EventState
{
    public Vector2 CameraPosition { get; set; }
    public float CameraScale { get; set; }
    public float CameraRotation { get; set; }
}