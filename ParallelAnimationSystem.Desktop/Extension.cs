using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Rendering.OpenGL;
using ParallelAnimationSystem.Rendering.OpenGLES;

namespace ParallelAnimationSystem.Desktop;

public static class Extension
{
    public static IServiceCollection AddPlatform<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TGlfw>(this IServiceCollection services, RenderingBackend backend) 
        where TGlfw : GlfwService
    {
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
        });

        services.AddScoped<GlfwService, TGlfw>();

        var builder = services.AddPAS();
        
        switch (backend)
        {
            case RenderingBackend.OpenGL:
                builder.UseOpenGLRenderer();
                break;
            case RenderingBackend.OpenGLES:
                builder.UseOpenGLESRenderer();
                break;
        }
        
        return services;
    }
}