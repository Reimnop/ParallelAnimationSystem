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

var widthOption = new Option<int>(
    aliases: ["-w", "--width"],
    description: "Width of the window",
    getDefaultValue: () => 1366
);

var heightOption = new Option<int>(
    aliases: ["-h", "--height"],
    description: "Height of the window",
    getDefaultValue: () => 768
);

var vsyncOption = new Option<bool>(
    aliases: ["--vsync"],
    description: "Enable VSync",
    getDefaultValue: () => true
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

var speedOption = new Option<float>(
    aliases: ["--speed"],
    description: "Sets the playback speed",
    getDefaultValue: () => 1.0f
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

var backgroundOpacityOption = new Option<float>(
    aliases: ["--background-opacity"],
    description: "Sets the background opacity (0.0 - 1.0)",
    getDefaultValue: () => 1.0f
);

var transparentOption = new Option<bool>(
    aliases: ["--transparent"],
    description: "Enable window transparency",
    getDefaultValue: () => false
);

var resizableOption = new Option<bool>(
    aliases: ["--resizable"],
    description: "Enable window resizing",
    getDefaultValue: () => true
);

var borderlessOption = new Option<bool>(
    aliases: ["--borderless"],
    description: "Enable borderless window",
    getDefaultValue: () => false
);

var floatingOption = new Option<bool>(
    aliases: ["--floating"],
    description: "Enable floating window (always on top)",
    getDefaultValue: () => false
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
rootCommand.AddOption(widthOption);
rootCommand.AddOption(heightOption);
rootCommand.AddOption(vsyncOption);
rootCommand.AddOption(workersOption);
rootCommand.AddOption(seedOption);
rootCommand.AddOption(speedOption);
rootCommand.AddOption(backendOption);
rootCommand.AddOption(lockAspectOption);
rootCommand.AddOption(backgroundOpacityOption);
rootCommand.AddOption(transparentOption);
rootCommand.AddOption(resizableOption);
rootCommand.AddOption(borderlessOption);
rootCommand.AddOption(floatingOption);
rootCommand.AddOption(postProcessingOption);
rootCommand.AddOption(textRenderingOption);

rootCommand.SetHandler(context =>
{
    var beatmapPath = context.ParseResult.GetValueForOption(beatmapOption)!;
    var audioPath = context.ParseResult.GetValueForOption(audioOption)!;
    var width = context.ParseResult.GetValueForOption(widthOption);
    var height = context.ParseResult.GetValueForOption(heightOption);
    var vsync = context.ParseResult.GetValueForOption(vsyncOption);
    var workerCount = context.ParseResult.GetValueForOption(workersOption);
    var seed = context.ParseResult.GetValueForOption(seedOption);
    var speed = context.ParseResult.GetValueForOption(speedOption);
    var backend = context.ParseResult.GetValueForOption(backendOption);
    var lockAspectRatio = context.ParseResult.GetValueForOption(lockAspectOption);
    var backgroundOpacity = context.ParseResult.GetValueForOption(backgroundOpacityOption);
    var transparent = context.ParseResult.GetValueForOption(transparentOption);
    var resizable = context.ParseResult.GetValueForOption(resizableOption);
    var borderless = context.ParseResult.GetValueForOption(borderlessOption);
    var floating = context.ParseResult.GetValueForOption(floatingOption);
    var enablePostProcessing = context.ParseResult.GetValueForOption(postProcessingOption);
    var enableTextRendering = context.ParseResult.GetValueForOption(textRenderingOption);
    
    DesktopStartup.ConsumeOptions(
        beatmapPath,
        audioPath,
        width,
        height,
        vsync,
        workerCount,
        seed,
        speed,
        backend,
        lockAspectRatio,
        backgroundOpacity,
        transparent,
        resizable,
        borderless,
        floating,
        enablePostProcessing,
        enableTextRendering
    );
});

return rootCommand.Invoke(args);