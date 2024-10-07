using System.Text.Json.Nodes;
using Pamx.Common;
using Pamx.Ls;
using Pamx.Vg;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Data;

namespace ParallelAnimationSystem.Wasm;

public class WasmMediaProvider : IMediaProvider
{
    public IBeatmap LoadBeatmap(out BeatmapFormat format)
    {
        var beatmapJsonStr = JsApi.GetBeatmapData();
        var beatmapJson = JsonNode.Parse(beatmapJsonStr);
        if (beatmapJson is not JsonObject jsonObject)
            throw new InvalidOperationException("Failed to parse beatmap JSON");

        var formatStr = JsApi.GetBeatmapFormat();
        format = formatStr switch
        {
            "lsb" => BeatmapFormat.Lsb,
            "vgd" => BeatmapFormat.Vgd,
            _ => throw new InvalidOperationException($"Unsupported beatmap format '{formatStr}'")
        };
        
        return format switch
        {
            BeatmapFormat.Lsb => LsDeserialization.DeserializeBeatmap(jsonObject),
            BeatmapFormat.Vgd => VgDeserialization.DeserializeBeatmap(jsonObject),
            _ => throw new InvalidOperationException($"Unsupported beatmap format '{format}'"),
        };
    }
}