using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Platform.OpenGL;
using ParallelAnimationSystem.Rendering.Common;

namespace ParallelAnimationSystem.Rendering.OpenGLES;

public static class PASOptionsBuilderExtension
{
    public static PASBuilder UseOpenGLESRenderer(this PASBuilder builder)
    {
        var services = builder.Services;
        
        services.AddSingleton<IRenderingFactory, RenderingFactory>();
        services.AddScoped<IRenderer, Renderer>();

        services.AddSingleton(new OpenGLSettings
        {
            MajorVersion = 3,
            MinorVersion = 0,
            IsES = true
        });
        
        builder.UseResourceSourceFactory(() => new EmbeddedResourceSource(typeof(PASOptionsBuilderExtension).Assembly));
        
        return builder;
    }
}