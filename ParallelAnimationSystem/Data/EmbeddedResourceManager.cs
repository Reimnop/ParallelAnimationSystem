namespace ParallelAnimationSystem.Data;

public class EmbeddedResourceManager(string graphicsApiName) : IResourceManager
{
    public Stream LoadResource(string name)
    {
        var path = $"ParallelAnimationSystem.Resources.{name.Replace('/', '.')}";
        var assembly = typeof(EmbeddedResourceManager).Assembly;
        var stream = assembly.GetManifestResourceStream(path);
        if (stream is null)
            throw new ArgumentException($"Resource '{name}' not found");
        return stream;
    }

    public Stream LoadGraphicsResource(string name)
        => LoadResource($"{graphicsApiName}/{name}");
}