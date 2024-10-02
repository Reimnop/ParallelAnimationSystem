namespace ParallelAnimationSystem.Data;

public class MergedResourceManager(IEnumerable<IResourceManager> resourceManagers) : IResourceManager
{
    private readonly List<IResourceManager> resourceManagers = resourceManagers.ToList();
    
    public Stream? LoadResource(string name)
    {
        foreach (var resourceManager in resourceManagers)
        {
            var stream = resourceManager.LoadResource(name);
            if (stream is not null)
                return stream;
        }
        
        return null;
    }
}