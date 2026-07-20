using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Platform.OpenGL;
using ParallelAnimationSystem.Rendering.Common;

namespace ParallelAnimationSystem.Rendering.OpenGL;

public static class PASOptionsBuilderExtension
{
    public static PASBuilder UseOpenGLRenderer(this PASBuilder builder)
    {
        var services = builder.Services;
        
        services.AddSingleton<IRenderingFactory, RenderingFactory>();
        services.AddSingleton<BuiltinRenderingPrimitive>();
        services.AddScoped<IRenderer, Renderer>();

        services.AddSingleton(new OpenGLSettings
        {
            MajorVersion = 4,
            MinorVersion = 6,
            IsES = false
        });
        
        builder.UseResourceSourceFactory(() => new EmbeddedResourceSource(typeof(PASOptionsBuilderExtension).Assembly));
        
        return builder;
    }
}