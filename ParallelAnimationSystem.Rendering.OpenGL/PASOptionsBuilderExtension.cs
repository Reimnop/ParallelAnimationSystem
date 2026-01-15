using ParallelAnimationSystem.Core;

namespace ParallelAnimationSystem.Rendering.OpenGL;

public static class PASOptionsBuilderExtension
{
    public static PASOptionsBuilder UseOpenGLRenderer(this PASOptionsBuilder builder)
        => builder
            .UseRenderer<Renderer>()
            .AddResourceSource(new EmbeddedResourceSource(typeof(PASOptionsBuilderExtension).Assembly));
}