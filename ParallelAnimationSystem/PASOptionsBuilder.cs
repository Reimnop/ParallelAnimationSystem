using System.Diagnostics.CodeAnalysis;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Util;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem;

public class PASOptionsBuilder
{
    private AppSettings? appSettings;
    private readonly List<Func<IResourceSource>> resourceSourceFactories = [];
    private ServiceDefinition<IWindowManager>? windowManagerDefinition;
    private ServiceDefinition<IMediaProvider>? mediaProviderDefinition;
    private ServiceDefinition<IRenderer>? rendererDefinition;

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
    
    public PASOptionsBuilder UseWindowManager(ServiceDefinition<IWindowManager> definition)
    {
        windowManagerDefinition = definition;
        return this;
    }
    
    public PASOptionsBuilder UseWindowManager<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        where T : IWindowManager
        => UseWindowManager(typeof(T));
    
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

    public PASOptions Build()
    {
        if (appSettings is null)
            ThrowNotSet(nameof(appSettings));
        if (windowManagerDefinition is null)
            ThrowNotSet(nameof(windowManagerDefinition));
        if (mediaProviderDefinition is null)
            ThrowNotSet(nameof(mediaProviderDefinition));
        if (rendererDefinition is null)
            ThrowNotSet(nameof(rendererDefinition));

        return new PASOptions
        {
            AppSettings = appSettings,
            ResourceSourceFactories = resourceSourceFactories,
            WindowManagerDefinition = windowManagerDefinition,
            MediaProviderDefinition = mediaProviderDefinition,
            RendererDefinition = rendererDefinition,
        };
    }
    
    [DoesNotReturn]
    private void ThrowNotSet(string name)
        => throw new InvalidOperationException($"'{name}' is not set");
}