namespace ParallelAnimationSystem.Core;

public interface IResourceSource
{
    Stream? OpenResource(string resourceName);

    byte[]? ReadResourceBytes(string resourceName)
    {
        using var stream = OpenResource(resourceName);
        if (stream is null)
            return null;

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
    
    string? ReadResourceString(string resourceName)
    {
        using var stream = OpenResource(resourceName);
        if (stream is null)
            return null;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}