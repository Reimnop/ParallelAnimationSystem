using System.Text;
using System.Text.Json.Nodes;
using Pamx.Common;
using Pamx.Ls;
using Pamx.Vg;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Data;

namespace ParallelAnimationSystem.Android;

public class AndroidMediaProvider(BeatmapContext context) : IMediaProvider
{
    public IBeatmap LoadBeatmap(out BeatmapFormat format)
    {
        var json = JsonNode.Parse(context.Data);
        if (json is not JsonObject jsonObject)
            throw new InvalidDataException("Invalid beatmap JSON");

        format = context.Format;
        return format switch
        {
            BeatmapFormat.Lsb => LsDeserialization.DeserializeBeatmap(jsonObject),
            BeatmapFormat.Vgd => VgDeserialization.DeserializeBeatmap(jsonObject),
            _ => throw new NotSupportedException($"Unsupported beatmap format '{format}'")
        };
    }
}