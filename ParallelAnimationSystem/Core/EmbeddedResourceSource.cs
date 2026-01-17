using System.Reflection;

namespace ParallelAnimationSystem.Core;

public class EmbeddedResourceSource(Assembly assembly) : IResourceSource
{
    public Stream? OpenResource(string resourceName)
    {
        // Replace slashes with dots for embedded resource naming
        var resourcePath = $"{assembly.GetName().Name}.Resources.{resourceName.Replace('/', '.')}";
        return assembly.GetManifestResourceStream(resourcePath);
    }
}
