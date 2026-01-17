using Pamx.Common;

namespace ParallelAnimationSystem.Core;

public interface IMediaProvider
{
    IBeatmap LoadBeatmap(out BeatmapFormat format);
}