namespace ParallelAnimationSystem.Core.Beatmap;

public class Prefab
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public BeatmapObject RootObject { get; } = new(
        string.Empty,
        new BeatmapObjectData([], [], [], []));
}