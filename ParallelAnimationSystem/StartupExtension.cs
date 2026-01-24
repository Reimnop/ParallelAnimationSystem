using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.DebugUI;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem;

public static class StartupExtension
{
    public static IServiceCollection AddPAS(this IServiceCollection services, Action<PASOptionsBuilder> builder)
        => services.AddPAS<AppCore>(builder);
    
    public static IServiceCollection AddPAS<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAppCore>(this IServiceCollection services, Action<PASOptionsBuilder> builder) where TAppCore : AppCore
    {
        var optionsBuilder = new PASOptionsBuilder(services);
        builder(optionsBuilder);
        var options = optionsBuilder.Build();
        return services.AddPAS<TAppCore>(options);
    }
    
    public static IServiceCollection AddPAS<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAppCore>(this IServiceCollection services, PASOptions options) where TAppCore : AppCore
    {
        services.AddSingleton(options.AppSettings);
        
        // Add beatmap runner
        services.AddSingleton<AppCore, TAppCore>();
        
        // Add external services
        options.WindowDefinition.RegisterToServiceCollection(services, ServiceLifetime.Singleton);
        options.MediaProviderDefinition.RegisterToServiceCollection(services, ServiceLifetime.Singleton);
        options.RendererDefinition.RegisterToServiceCollection(services, ServiceLifetime.Singleton);
        options.RenderingFactoryDefinition.RegisterToServiceCollection(services, ServiceLifetime.Singleton);
        
        // Copy resource source factories to our own list
        var resourceSourceFactories = new List<Func<IResourceSource>>();
        resourceSourceFactories.AddRange(options.ResourceSourceFactories);
        
        // Add our own sources
        resourceSourceFactories.Add(() => new EmbeddedResourceSource(typeof(StartupExtension).Assembly));
        
        // Add resource loader
        services.AddSingleton(_ => new ResourceLoader(resourceSourceFactories));
        
        // Add ImGui
        services.AddSingleton<ImGuiContext>();
        services.AddSingleton<ImGuiBackend>();
        
        // Add migrations
        services.AddTransient<LsMigration>();
        services.AddTransient<VgMigration>();
        
        return services;
    }

    public static AppCore InitializeAppCore(this IServiceProvider serviceProvider)
        => serviceProvider.GetRequiredService<AppCore>();

    public static IRenderer InitializeRenderer(this IServiceProvider serviceProvider)
        => serviceProvider.GetRequiredService<IRenderer>();
    
    public static ImGuiBackend InitializeImGui(this IServiceProvider serviceProvider)
        => serviceProvider.GetRequiredService<ImGuiBackend>();
}
