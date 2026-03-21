using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Rendering.Common;
using ParallelAnimationSystem.Windowing.OpenGL;

#if DEBUG
using ParallelAnimationSystem.DebugStuff;
using ParallelAnimationSystem.Rendering.OpenGL.DebugStuff;
#endif

namespace ParallelAnimationSystem.Rendering.OpenGL;

public static class PASOptionsBuilderExtension
{
    public static PASBuilder UseOpenGLRenderer(this PASBuilder builder)
    {
        var services = builder.Services;
        
        services.AddSingleton<IRenderingFactory, RenderingFactory>();
        services.AddScoped<IRenderer, Renderer>();

        services.AddSingleton(new OpenGLSettings
        {
            MajorVersion = 4,
            MinorVersion = 6,
            IsES = false
        });
        
        builder.UseResourceSourceFactory(() => new EmbeddedResourceSource(typeof(PASOptionsBuilderExtension).Assembly));
        
#if DEBUG
        // Add ImGui renderer backend
        services.AddScoped<IImGuiRendererBackend, ImGuiRendererBackend>();
#endif
        
        return builder;
    }
}