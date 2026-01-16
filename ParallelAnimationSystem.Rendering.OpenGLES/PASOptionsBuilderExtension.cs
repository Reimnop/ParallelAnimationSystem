using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;

namespace ParallelAnimationSystem.Rendering.OpenGLES;

public static class PASOptionsBuilderExtension
{
    public static PASOptionsBuilder UseOpenGLESRenderer(this PASOptionsBuilder builder)
    {
        builder
            .UseRenderer<Renderer>()
            .UseRenderingFactory<RenderingFactory>()
            .AddResourceSource(new EmbeddedResourceSource(typeof(PASOptionsBuilderExtension).Assembly));
        
        builder.Services.AddSingleton<IncomingResourceQueue>();
        
        return builder;
    }
}