using ParallelAnimationSystem.Core;

namespace ParallelAnimationSystem.Resources.Raw;

public static class Extension
{
    public static ResourceSourceFactories AddRawResources(this ResourceSourceFactories factories)
    {
        factories.Add(() => new EmbeddedResourceSource(typeof(Extension).Assembly));
        return factories;
    }
}
