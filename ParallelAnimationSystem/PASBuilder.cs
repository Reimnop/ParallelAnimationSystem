using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Core;

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

    public PASBuilder UseResourceSourceFactory(Func<IResourceSource> factory)
    {
        resourceSourceFactories.Add(factory);
        return this;
    }
}