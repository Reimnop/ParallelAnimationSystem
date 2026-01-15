namespace ParallelAnimationSystem.Core;

public class ResourceLoader
{
    private readonly List<IResourceSource> resourceSources;

    internal ResourceLoader(IEnumerable<Func<IResourceSource>> resourceSourceFactories)
    {
        resourceSources = resourceSourceFactories.Select(factory => factory()).ToList();
    }
    
    public Stream? OpenResource(string resourceName)
    {
        foreach (var source in resourceSources)
        {
            var stream = source.OpenResource(resourceName);
            if (stream is not null)
                return stream;
        }

        return null;
    }
    
    public byte[]? ReadResourceBytes(string resourceName)
    {
        using var stream = OpenResource(resourceName);
        if (stream is null)
            return null;

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
    
    public string? ReadResourceString(string resourceName)
    {
        using var stream = OpenResource(resourceName);
        if (stream is null)
            return null;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}