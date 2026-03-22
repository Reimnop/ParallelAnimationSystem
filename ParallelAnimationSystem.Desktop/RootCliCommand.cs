using DotMake.CommandLine;

namespace ParallelAnimationSystem.Desktop;

[CliCommand(
    Description = "Common options for all commands",
    ShortFormAutoGenerate = CliNameAutoGenerate.None,
    Children = [typeof(RunCliCommand), typeof(RenderCliCommand)])]
public class RootCliCommand
{
    [CliOption(Name = "beatmap", Alias = "b", Description = "Path to the beatmap file (.lsb or .vgd)")]
    public required string BeatmapPath { get; set; }

    [CliOption(Name = "audio", Alias = "a", Description = "Path to the audio file")]
    public required string AudioPath { get; set; }

    [CliOption(Name = "width", Alias = "w", Description = "Width of the window")]
    public int Width { get; set; } = 1366;

    [CliOption(Name = "height", Alias = "h", Description = "Height of the window")]
    public int Height { get; set; } = 768;

    [CliOption(Name = "use-egl", Description = "Use EGL for context creation")]
    public bool UseEgl { get; set; }

    [CliOption(Name = "seed", Description = "Seed for the random number generator")]
    public ulong? Seed { get; set; }

    [CliOption(Name = "backend", Description = "Sets the rendering backend to use")]
    public RenderingBackend Backend { get; set; } = RenderingBackend.OpenGL;

    [CliOption(Name = "post-processing", Description = "Enable post-processing")]
    public bool EnablePostProcessing { get; set; } = true;

    [CliOption(Name = "text-rendering", Description = "Enable text rendering")]
    public bool EnableTextRendering { get; set; } = true;

    [CliOption(Name = "start-time", Description = "Sets the playback start time")]
    public float StartTime { get; set; }
}