using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.DebugUI;
using ParallelAnimationSystem.Windowing.OpenGL;

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
        
        // Add ImGui renderer backend
        builder.Services.AddSingleton<IImGuiRendererBackend, ImGuiRendererBackend>();
        
        return builder;
    }
}