using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Rendering.OpenGL;
using ParallelAnimationSystem.Rendering.OpenGLES;
using ParallelAnimationSystem.Windowing;

#if DEBUG
using ParallelAnimationSystem.DebugStuff;
using ParallelAnimationSystem.Desktop.DebugStuff;
#endif

namespace ParallelAnimationSystem.Desktop;

public static class Extension
{
    public static IServiceCollection AddPlatform<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TWindow,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TGlfw>(
        this IServiceCollection services,
        RenderingBackend backend,
        bool lockAspectRatio,
        bool enablePostProcessing,
        bool enableTextRendering) 
        where TWindow : IWindow
        where TGlfw : GlfwService
    {
        var appSettings = new AppSettings
        {
            AspectRatio = lockAspectRatio ? 16f / 9f : null,
            EnablePostProcessing = enablePostProcessing,
            EnableTextRendering = enableTextRendering,
        };
        
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
        });

        services.AddScoped<GlfwService, TGlfw>();
        
        services.AddPAS(builder =>
        {
            builder.UseAppSettings(appSettings);
            builder.UseWindow<TWindow>();
            switch (backend)
            {
                case RenderingBackend.OpenGL:
                    builder.UseOpenGLRenderer();
                    break;
                case RenderingBackend.OpenGLES:
                    builder.UseOpenGLESRenderer();
                    break;
            }
        });
        
#if DEBUG
        // Add ImGui platform backend
        services.AddSingleton<IImGuiPlatformBackend, ImGuiPlatformBackend>();
#endif
        
        return services;
    }
}