namespace ParallelAnimationSystem.Windowing.OpenGL;

public class OpenGLSettings : IGraphicsApiSettings
{
    public required int MajorVersion { get; init; }
    public required int MinorVersion { get; init; }
    public required bool IsES { get; init; }
}
