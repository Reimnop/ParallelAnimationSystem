using System.Text.Json.Nodes;
using Pamx.Common;
using Pamx.Ls;
using Pamx.Vg;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Data;

namespace ParallelAnimationSystem.Wasm;

public class WasmMediaProvider(string beatmapData, BeatmapFormat beatmapFormat) : IMediaProvider
{
    public IBeatmap LoadBeatmap(out BeatmapFormat format)
    {
        var beatmapJson = JsonNode.Parse(beatmapData);
        if (beatmapJson is not JsonObject jsonObject)
            throw new InvalidOperationException("Failed to parse beatmap JSON");
        
        format = beatmapFormat;
        
        return beatmapFormat switch
        {
            BeatmapFormat.Lsb => LsDeserialization.DeserializeBeatmap(jsonObject),
            BeatmapFormat.Vgd => VgDeserialization.DeserializeBeatmap(jsonObject),
            _ => throw new InvalidOperationException($"Unsupported beatmap format '{beatmapFormat}'"),
        };
    }
}