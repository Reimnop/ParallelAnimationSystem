using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Core.Service;
using ParallelAnimationSystem.Rendering;

#if DEBUG
using ParallelAnimationSystem.DebugStuff;
#endif

namespace ParallelAnimationSystem;

public static class StartupExtension
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPAS(Action<PASOptionsBuilder> builder)
            => services.AddPAS<AppCore>(builder);

        public IServiceCollection AddPAS<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAppCore>(Action<PASOptionsBuilder> builder) where TAppCore : AppCore
        {
            var optionsBuilder = new PASOptionsBuilder(services);
            builder(optionsBuilder);
            var options = optionsBuilder.Build();
            return services.AddPAS<TAppCore>(options);
        }

        public IServiceCollection AddPAS<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAppCore>(PASOptions options) where TAppCore : AppCore
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
        
            // Add main services
            services.AddSingleton<AnimationPipeline>();
            services.AddSingleton<Timeline>();
            services.AddSingleton<ObjectSourceManager>();
            services.AddSingleton<PlaybackObjectContainer>();

            services.AddSingleton<ThemeManager>();
            services.AddSingleton<PlaybackThemeContainer>();

            services.AddSingleton<EventManager>();

            services.AddSingleton<RandomSeedService>();
            services.AddSingleton<TextRenderingService>();
            services.AddSingleton<MeshService>();
            services.AddSingleton<BeatmapService>();
        
#if DEBUG
            // Add ImGui
            services.AddSingleton<ImGuiContext>();
            services.AddSingleton<ImGuiBackend>();
#endif
        
            // Add migrations
            services.AddTransient<LsMigration>();
            services.AddTransient<VgMigration>();
        
            return services;
        }
    }

    extension(IServiceProvider serviceProvider)
    {
        public AppCore InitializeAppCore()
            => serviceProvider.GetRequiredService<AppCore>();

        public IRenderer InitializeRenderer()
            => serviceProvider.GetRequiredService<IRenderer>();

#if DEBUG
        public ImGuiBackend InitializeImGui()
            => serviceProvider.GetRequiredService<ImGuiBackend>();
#endif
    }
}
