namespace ParallelAnimationSystem.Core.Beatmap;

public record struct ObjectId(string String, int Numeric)
{
    public int Hash => hashCache ??= ComputeHash();
    
    private int? hashCache = null;
    
    private int ComputeHash()
    {
        var hash = 2166136261u;
        foreach (var c in String)
            hash = (hash ^ c) * 16777619u;
        return unchecked((int) hash);
    }
}