namespace ParallelAnimationSystem.Data;

public interface IResourceManager
{
    Stream? LoadResource(string name);

    string LoadResourceString(string name)
    {
        using var stream = LoadResource(name);
        if (stream is null)
            throw new ArgumentException($"Resource '{name}' not found");
        
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}