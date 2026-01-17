using System.Text.Json.Nodes;
using Pamx.Common;
using Pamx.Ls;
using Pamx.Vg;
using ParallelAnimationSystem.Core;

namespace ParallelAnimationSystem.Desktop;

public class DesktopMediaProvider(MediaContext context) : IMediaProvider
{
    public IBeatmap LoadBeatmap(out BeatmapFormat format)
    {
        var fileExtension = Path.GetExtension(context.BeatmapPath);
        format = fileExtension switch
        {
            ".lsb" => BeatmapFormat.Lsb,
            ".vgd" => BeatmapFormat.Vgd,
            _ => throw new NotSupportedException($"Unsupported beatmap type '{fileExtension}'"),
        };
        
        var jsonString = File.ReadAllText(context.BeatmapPath);
        var json = JsonNode.Parse(jsonString);
        if (json is not JsonObject jsonObject)
            throw new InvalidDataException("Invalid beatmap JSON");
        
        var beatmap = format switch
        {
            BeatmapFormat.Lsb => LsDeserialization.DeserializeBeatmap(jsonObject),
            BeatmapFormat.Vgd => VgDeserialization.DeserializeBeatmap(jsonObject),
            _ => throw new NotSupportedException($"Unsupported beatmap format '{format}'"),
        };
        
        return beatmap;
    }
}