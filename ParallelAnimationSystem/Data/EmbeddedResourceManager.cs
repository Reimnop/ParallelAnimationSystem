using System.Reflection;

namespace ParallelAnimationSystem.Data;

public class EmbeddedResourceManager(Assembly assembly) : IResourceManager
{
    public Stream? LoadResource(string name)
    {
        var path = $"ParallelAnimationSystem.Resources.{name.Replace('/', '.')}";
        return assembly.GetManifestResourceStream(path);
    }
}