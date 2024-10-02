using CommandLine;
using ParallelAnimationSystem.Core;

namespace ParallelAnimationSystem.Desktop;

public class Options
{
    [Option('l', "level", Required = true, HelpText = "Path to the level file (.lsb or .vgd).")]
    public required string LevelPath { get; set; }
    
    [Option('a', "audio", Required = true, HelpText = "Path to the audio file.")]
    public required string AudioPath { get; set; }
    
    [Option("format", Required = false, HelpText = "The format of the level file (lsb/vgd).")]
    public BeatmapFormat? Format { get; set; }
    
    [Option("vsync", Required = false, HelpText = "Enable VSync.")]
    public bool VSync { get; set; }
    
    [Option("workers", Required = false, Default = 4, HelpText = "Number of worker threads, set to -1 to use all available threads.")]
    public int WorkerCount { get; set; }
    
    [Option("seed", Required = false, Default = -1, HelpText = "Seed for the random number generator, set to -1 to use a random seed.")]
    public long Seed { get; set; }
    
    [Option("speed", Required = false, Default = 1.0f, HelpText = "Sets the playback speed.")]
    public float Speed { get; set; } = 1.0f;
    
    [Option("backend", Required = false, Default = "opengl", HelpText = "Sets the rendering backend to use (opengl/opengles).")]
    public required string Backend { get; set; }
    
    [Option("experimental-enable-text-rendering", Required = false, Default = false, HelpText = "Enable experimental text rendering.")]
    public bool EnableTextRendering { get; set; }
}