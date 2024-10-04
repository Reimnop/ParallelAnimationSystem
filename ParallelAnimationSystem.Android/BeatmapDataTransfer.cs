namespace ParallelAnimationSystem.Android;

public static class BeatmapDataTransfer
{
    private static byte[]? beatmapData;
    private static byte[]? audioData;
    
    public static void Put(byte[] beatmapData, byte[] audioData)
    {
        BeatmapDataTransfer.beatmapData = beatmapData;
        BeatmapDataTransfer.audioData = audioData;
    }
    
    public static (byte[] BeatmapData, byte[] AudioData)? GetBeatmapData()
    {
        if (beatmapData is null || audioData is null)
            return null;
        var value = (beatmapData, audioData);
        beatmapData = null;
        audioData = null;
        return value;
    }
}