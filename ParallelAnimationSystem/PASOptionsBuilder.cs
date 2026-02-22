using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Util;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem;

public class PASOptionsBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services;
    
    private AppSettings? appSettings;
    private readonly List<Func<IResourceSource>> resourceSourceFactories = [];
    private ServiceDefinition<IWindow>? windowDefinition;
    private ServiceDefinition<IMediaProvider>? mediaProviderDefinition;
    private ServiceDefinition<IRenderer>? rendererDefinition;
    private ServiceDefinition<IRenderingFactory>? renderingFactoryDefinition;

    public PASOptionsBuilder UseAppSettings(AppSettings settings)
    {
        appSettings = settings;
        return this;
    }
    
    public PASOptionsBuilder AddResourceSource(Func<IResourceSource> factory)
    {
        resourceSourceFactories.Add(factory);
        return this;
    }

    public PASOptionsBuilder AddResourceSource<T>() where T : IResourceSource, new()
        => AddResourceSource(() => new T());
    
    public PASOptionsBuilder AddResourceSource(IResourceSource resourceSource)
        => AddResourceSource(() => resourceSource);
    
    public PASOptionsBuilder UseWindow(ServiceDefinition<IWindow> definition)
    {
        windowDefinition = definition;
        return this;
    }
    
    public PASOptionsBuilder UseWindow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        where T : IWindow
        => UseWindow(typeof(T));
    
    public PASOptionsBuilder UseMediaProvider(ServiceDefinition<IMediaProvider> definition)
    {
        mediaProviderDefinition = definition;
        return this;
    }
    
    public PASOptionsBuilder UseMediaProvider<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        where T : IMediaProvider
        => UseMediaProvider(typeof(T));
    
    public PASOptionsBuilder UseRenderer(ServiceDefinition<IRenderer> definition)
    {
        rendererDefinition = definition;
        return this;
    }
    
    public PASOptionsBuilder UseRenderer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        where T : IRenderer
        => UseRenderer(typeof(T));
    
    public PASOptionsBuilder UseRenderingFactory(ServiceDefinition<IRenderingFactory> definition)
    {
        renderingFactoryDefinition = definition;
        return this;
    }
    
    public PASOptionsBuilder UseRenderingFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        where T : IRenderingFactory
        => UseRenderingFactory(typeof(T));

    public PASOptions Build()
    {
        if (appSettings is null)
            ThrowNotSet(nameof(appSettings));
        if (windowDefinition is null)
            ThrowNotSet(nameof(windowDefinition));
        if (mediaProviderDefinition is null)
            ThrowNotSet(nameof(mediaProviderDefinition));
        if (rendererDefinition is null)
            ThrowNotSet(nameof(rendererDefinition));
        if (renderingFactoryDefinition is null)
            ThrowNotSet(nameof(renderingFactoryDefinition));

        return new PASOptions
        {
            AppSettings = appSettings,
            ResourceSourceFactories = resourceSourceFactories,
            WindowDefinition = windowDefinition,
            MediaProviderDefinition = mediaProviderDefinition,
            RendererDefinition = rendererDefinition,
            RenderingFactoryDefinition = renderingFactoryDefinition,
        };
    }
    
    [DoesNotReturn]
    private void ThrowNotSet(string name)
        => throw new InvalidOperationException($"'{name}' is not set");
}