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
        {
            var optionsBuilder = new PASOptionsBuilder(services);
            builder(optionsBuilder);
            var options = optionsBuilder.Build();
            return services.AddPAS(options);
        }

        public IServiceCollection AddPAS(PASOptions options)
        {
            services.AddSingleton(options.AppSettings);
            
            // Everything related to resources are singletons
            options.RenderingFactoryDefinition.RegisterToServiceCollection(services, ServiceLifetime.Singleton);
            
            // Add resource loader with all resource source factories
            services.AddSingleton(_ => new ResourceLoader(options.ResourceSourceFactories
                .Append(() => new EmbeddedResourceSource(typeof(StartupExtension).Assembly))));
            
            // These manage rendering resources, so they should be singletons
            services.AddSingleton<MeshService>();
            services.AddSingleton<FontService>();
        
            // Add rendering services
            options.WindowDefinition.RegisterToServiceCollection(services, ServiceLifetime.Scoped);
            options.RendererDefinition.RegisterToServiceCollection(services, ServiceLifetime.Scoped);
        
            // Add main services
            services.AddScoped<AppCore>();
            services.AddScoped<AnimationPipeline>();
            services.AddScoped<PlaybackObjectSortingService>();
            services.AddScoped<Timeline>();
            services.AddScoped<ObjectSourceManager>();
            services.AddScoped<PlaybackObjectContainer>();
            services.AddScoped<ThemeManager>();
            services.AddScoped<PlaybackThemeContainer>();
            services.AddScoped<EventManager>();
            services.AddScoped<RandomSeedService>();
            services.AddScoped<BeatmapService>();
        
#if DEBUG
            // Add ImGui
            services.AddScoped<ImGuiContext>();
            services.AddScoped<ImGuiBackend>();
#endif
        
            // Add migrations
            services.AddTransient<LsMigration>();
            services.AddTransient<VgMigration>();
        
            return services;
        }
    }
}
