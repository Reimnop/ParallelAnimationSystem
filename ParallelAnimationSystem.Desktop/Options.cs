using CommandLine;

namespace ParallelAnimationSystem.Desktop;

public class Options
{
    [Option('b', "beatmap", Required = true, HelpText = "Path to the beatmap file (.lsb or .vgd).")]
    public required string BeatmapPath { get; set; }
    
    [Option('a', "audio", Required = true, HelpText = "Path to the audio file.")]
    public required string AudioPath { get; set; }
    
    [Option("vsync", Required = false, Default = true, HelpText = "Enable VSync.")]
    public bool? VSync { get; set; }
    
    [Option("workers", Required = false, Default = 4, HelpText = "Number of worker threads, set to -1 to use all available threads.")]
    public int WorkerCount { get; set; }
    
    [Option("seed", Required = false, Default = -1, HelpText = "Seed for the random number generator, set to -1 to use a random seed.")]
    public long Seed { get; set; }
    
    [Option("speed", Required = false, Default = 1.0f, HelpText = "Sets the playback speed.")]
    public float Speed { get; set; } = 1.0f;
    
    [Option("backend", Required = false, Default = RenderingBackend.OpenGL, HelpText = "Sets the rendering backend to use (opengl/opengles).")]
    public RenderingBackend? Backend { get; set; }
    
    [Option("lock-aspect", Required = false, Default = true, HelpText = "Lock the aspect ratio to 16:9.")]
    public bool? LockAspectRatio { get; set; }
    
    [Option("post-processing", Required = false, Default = true, HelpText = "Enable post-processing.")]
    public bool? EnablePostProcessing { get; set; }
    
    [Option("text-rendering", Required = false, Default = true, HelpText = "Enable text rendering.")]
    public bool? EnableTextRendering { get; set; }
}