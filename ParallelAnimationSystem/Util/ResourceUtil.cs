using ParallelAnimationSystem.Core;

namespace ParallelAnimationSystem.Util;

public static class ResourceUtil
{
    public static string ReadAllText(string path)
    {
        var assembly = typeof(App).Assembly;
        path = $"{assembly.GetName().Name}.{path}";
        
        using var stream = assembly.GetManifestResourceStream(path);
        if (stream is null)
            throw new InvalidOperationException($"File '{path}' not found");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    
    public static Stream ReadAsStream(string path)
    {
        var assembly = typeof(App).Assembly;
        path = $"{assembly.GetName().Name}.{path}";
        
        var stream = assembly.GetManifestResourceStream(path);
        if (stream is null)
            throw new InvalidOperationException($"File '{path}' not found");
        return stream;
    }
}