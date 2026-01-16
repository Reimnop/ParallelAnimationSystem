using System.CommandLine;
using ParallelAnimationSystem.Desktop;

var beatmapOption = new Option<string>(
    aliases: ["-b", "--beatmap"],
    description: "Path to the beatmap file (.lsb or .vgd)"
) 
{
    IsRequired = true
};

var audioOption = new Option<string>(
    aliases: ["-a", "--audio"],
    description: "Path to the audio file"
) 
{
    IsRequired = true
};

var vsyncOption = new Option<bool>(
    aliases: ["--vsync"],
    description: "Enable VSync",
    getDefaultValue: () => true
);

var useEglOption = new Option<bool>(
    aliases: ["--use-egl"],
    description: "Use EGL for context creation",
    getDefaultValue: () => false
);

var workersOption = new Option<int>(
    aliases: ["--workers"],
    description: "Number of worker threads, set to -1 to use all available threads",
    getDefaultValue: () => 4
);

var seedOption = new Option<long>(
    aliases: ["--seed"],
    description: "Seed for the random number generator, set to -1 to use a random seed",
    getDefaultValue: () => -1
);

var backendOption = new Option<RenderingBackend>(
    aliases: ["--backend"],
    description: "Sets the rendering backend to use",
    getDefaultValue: () => RenderingBackend.OpenGL
);

var lockAspectOption = new Option<bool>(
    aliases: ["--lock-aspect"],
    description: "Lock the aspect ratio to 16:9",
    getDefaultValue: () => true
);

var postProcessingOption = new Option<bool>(
    aliases: ["--post-processing"],
    description: "Enable post-processing",
    getDefaultValue: () => true
);

var textRenderingOption = new Option<bool>(
    aliases: ["--text-rendering"],
    description: "Enable text rendering",
    getDefaultValue: () => true
);

var rootCommand = new RootCommand("Parallel Animation System");
rootCommand.AddOption(beatmapOption);
rootCommand.AddOption(audioOption);
rootCommand.AddOption(vsyncOption);
rootCommand.AddOption(workersOption);
rootCommand.AddOption(seedOption);
rootCommand.AddOption(backendOption);
rootCommand.AddOption(lockAspectOption);
rootCommand.AddOption(postProcessingOption);
rootCommand.AddOption(textRenderingOption);

rootCommand.SetHandler(context =>
{
    var beatmapPath = context.ParseResult.GetValueForOption(beatmapOption)!;
    var audioPath = context.ParseResult.GetValueForOption(audioOption)!;
    var vsync = context.ParseResult.GetValueForOption(vsyncOption);
    var useEgl = context.ParseResult.GetValueForOption(useEglOption);
    var workerCount = context.ParseResult.GetValueForOption(workersOption);
    var seed = context.ParseResult.GetValueForOption(seedOption);
    var backend = context.ParseResult.GetValueForOption(backendOption);
    var lockAspectRatio = context.ParseResult.GetValueForOption(lockAspectOption);
    var enablePostProcessing = context.ParseResult.GetValueForOption(postProcessingOption);
    var enableTextRendering = context.ParseResult.GetValueForOption(textRenderingOption);
    
    DesktopStartup.ConsumeOptions(
        beatmapPath,
        audioPath,
        vsync,
        useEgl,
        workerCount,
        seed,
        backend,
        lockAspectRatio,
        enablePostProcessing,
        enableTextRendering
    );
});

return rootCommand.Invoke(args);