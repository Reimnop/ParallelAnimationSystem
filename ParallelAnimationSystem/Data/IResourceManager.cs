namespace ParallelAnimationSystem.Data;

public interface IResourceManager
{
    Stream LoadResource(string name);
    Stream LoadGraphicsResource(string name);

    string LoadResourceString(string name)
    {
        using var stream = LoadResource(name);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    
    string LoadGraphicsResourceString(string name)
    {
        using var stream = LoadGraphicsResource(name);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}