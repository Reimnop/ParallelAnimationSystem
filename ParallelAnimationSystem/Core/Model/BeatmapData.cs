namespace ParallelAnimationSystem.Core.Model;

public class BeatmapData
{
    public IdContainer<BeatmapObject> Objects { get; } = new();
    public IdContainer<BeatmapTheme> Themes { get; } = new();
    public BeatmapEventList Events { get; } = new();
}