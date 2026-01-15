using ParallelAnimationSystem.Core;

namespace ParallelAnimationSystem.Rendering.OpenGLES;

public static class PASOptionsBuilderExtension
{
    public static PASOptionsBuilder UseOpenGLESRenderer(this PASOptionsBuilder builder)
        => builder
            .UseRenderer<Renderer>()
            .AddResourceSource(new EmbeddedResourceSource(typeof(PASOptionsBuilderExtension).Assembly));
}