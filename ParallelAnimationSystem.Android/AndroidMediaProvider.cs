using System.Text.Json.Nodes;
using Pamx.Common;
using Pamx.Ls;
using Pamx.Vg;
using ParallelAnimationSystem.Core;
using ParallelAnimationSystem.Data;

namespace ParallelAnimationSystem.Android;

public class AndroidMediaProvider : IMediaProvider
{
    public IBeatmap LoadBeatmap(out BeatmapFormat format)
    {
        var assembly = typeof(AndroidMediaProvider).Assembly;
        using var stream = assembly.GetManifestResourceStream("ParallelAnimationSystem.Android.Beatmap.level.lsb");
        if (stream is null)
            throw new InvalidOperationException("Beatmap resource not found");
        
        using var reader = new StreamReader(stream);
        var jsonString = reader.ReadToEnd();
        var json = JsonNode.Parse(jsonString);
        if (json is not JsonObject jsonObject)
            throw new InvalidDataException("Invalid beatmap JSON");

        format = BeatmapFormat.Lsb;
        return LsDeserialization.DeserializeBeatmap(jsonObject);
    }
}