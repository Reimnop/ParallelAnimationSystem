using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Windowing.OpenGL;

#if DEBUG
using ParallelAnimationSystem.DebugStuff;
using ParallelAnimationSystem.Rendering.OpenGL.DebugStuff;
#endif

namespace ParallelAnimationSystem.Rendering.OpenGL;

public static class PASOptionsBuilderExtension
{
    public static PASOptionsBuilder UseOpenGLRenderer(this PASOptionsBuilder builder)
    {
        builder
            .UseRenderer<Renderer>()
            .UseRenderingFactory<RenderingFactory>()
            .AddResourceSource(new EmbeddedResourceSource(typeof(PASOptionsBuilderExtension).Assembly));

        builder.Services.AddSingleton<IncomingResourceQueue>();

        builder.Services.AddSingleton(new OpenGLSettings
        {
            MajorVersion = 4,
            MinorVersion = 6,
            IsES = false
        });
        
#if DEBUG
        // Add ImGui renderer backend
        builder.Services.AddSingleton<IImGuiRendererBackend, ImGuiRendererBackend>();
#endif
        
        return builder;
    }
}