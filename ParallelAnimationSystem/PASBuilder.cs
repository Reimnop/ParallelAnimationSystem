using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem;

public class PASBuilder
{
    public IServiceCollection Services { get; }
    
    private readonly ResourceSourceFactories resourceSourceFactories;

    internal PASBuilder(IServiceCollection services, ResourceSourceFactories resourceSourceFactories)
    {
        Services = services;
        this.resourceSourceFactories = resourceSourceFactories;
    }
    
    public PASBuilder UseAppSettings(AppSettings appSettings)
    {
        Services.AddSingleton(appSettings);
        return this;
    }
    
    public PASBuilder UseWindow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TWindow>() where TWindow : class, IWindow
    {
        Services.AddScoped<IWindow, TWindow>();
        return this;
    }

    public PASBuilder UseRenderQueue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRenderQueue>() where TRenderQueue : class, IRenderQueue
    {
        Services.AddSingleton<IRenderQueue, TRenderQueue>();
        return this;
    }

    public PASBuilder UseResourceSourceFactory(Func<IResourceSource> factory)
    {
        resourceSourceFactories.Add(factory);
        return this;
    }
}