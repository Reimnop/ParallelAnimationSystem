using CommandLine;
using ParallelAnimationSystem.Core;

namespace ParallelAnimationSystem;

public class Options
{
    [Option('l', "level", Required = true, HelpText = "Path to the level file (.lsb or .vgd)")]
    public required string LevelPath { get; set; }
    
    [Option('a', "audio", Required = true, HelpText = "Path to the audio file")]
    public required string AudioPath { get; set; }
    
    [Option('f', "format", Required = false, HelpText = "The format of the level file (lsb/vgd)")]
    public LevelFormat? Format { get; set; }
}