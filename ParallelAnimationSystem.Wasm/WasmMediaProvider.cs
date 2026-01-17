using System.Text.Json.Nodes;
using Pamx.Common;
using Pamx.Ls;
using Pamx.Vg;
using ParallelAnimationSystem.Core;

namespace ParallelAnimationSystem.Wasm;

public class WasmMediaProvider(MediaContext context) : IMediaProvider
{
    public IBeatmap LoadBeatmap(out BeatmapFormat format)
    {
        var beatmapJson = JsonNode.Parse(context.BeatmapData);
        if (beatmapJson is not JsonObject jsonObject)
            throw new InvalidOperationException("Failed to parse beatmap JSON");
        
        format = context.BeatmapFormat;
        
        return format switch
        {
            BeatmapFormat.Lsb => LsDeserialization.DeserializeBeatmap(jsonObject),
            BeatmapFormat.Vgd => VgDeserialization.DeserializeBeatmap(jsonObject),
            _ => throw new InvalidOperationException($"Unsupported beatmap format '{format}'"),
        };
    }
}