using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Rendering.Common;
using ParallelAnimationSystem.Windowing.OpenGL;

#if DEBUG
using ParallelAnimationSystem.DebugStuff;
using ParallelAnimationSystem.Rendering.OpenGLES.DebugStuff;
#endif

namespace ParallelAnimationSystem.Rendering.OpenGLES;

public static class PASOptionsBuilderExtension
{
    public static PASOptionsBuilder UseOpenGLESRenderer(this PASOptionsBuilder builder)
    {
        builder
            .UseRenderer<Renderer>()
            .UseRenderingFactory<RenderingFactory>()
            .AddResourceSource(new EmbeddedResourceSource(typeof(PASOptionsBuilderExtension).Assembly));

        builder.Services.AddSingleton(new OpenGLSettings
        {
            MajorVersion = 3,
            MinorVersion = 0,
            IsES = true,
        });
        
#if DEBUG
        // Add ImGui renderer backend
        builder.Services.AddScoped<IImGuiRendererBackend, ImGuiRendererBackend>();
#endif
        
        return builder;
    }
}