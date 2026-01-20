using System.Numerics;
using ImGuiNET;

namespace ParallelAnimationSystem.DebugUI;

public class ImGuiIOData
{
    public float DeltaTime { get; set; }
    public Vector2 DisplaySize { get; set; }
    public Vector2 MousePos { get; set; }
    public bool[] MouseDown { get; } = new bool[(int) ImGuiMouseButton.COUNT];
    public float MouseWheel { get; set; }
    public float MouseWheelH { get; set; }
}