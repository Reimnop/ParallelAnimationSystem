using Pamx.Common;
using ParallelAnimationSystem.Core;

namespace ParallelAnimationSystem.Data;

public interface IMediaProvider
{
    IBeatmap LoadBeatmap(out BeatmapFormat format);
}