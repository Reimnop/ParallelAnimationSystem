using Pamx;
using ParallelAnimationSystem.Core;

namespace ParallelAnimationSystem.Desktop;

public static class BeatmapHelper
{
    public static void ReadBeatmap(string beatmapPath, out string beatmapData, out BeatmapFormat beatmapFormat)
    {
        beatmapData = File.ReadAllText(beatmapPath);
        
        var extension = Path.GetExtension(beatmapPath).ToLowerInvariant();
        beatmapFormat = extension switch
        {
            ".lsb" => BeatmapFormat.Lsb,
            ".vgd" => BeatmapFormat.Vgd,
            _ => throw new NotSupportedException($"Unsupported beatmap extension '{extension}'")
        };
    }

    public static Stream OpenBeatmap(string beatmapPath, out BeatmapFormat beatmapFormat)
    {
        var extension = Path.GetExtension(beatmapPath).ToLowerInvariant();
        beatmapFormat = extension switch
        {
            ".lsb" => BeatmapFormat.Lsb,
            ".vgd" => BeatmapFormat.Vgd,
            _ => throw new NotSupportedException($"Unsupported beatmap extension '{extension}'")
        };

        return File.OpenRead(beatmapPath);
    }
}