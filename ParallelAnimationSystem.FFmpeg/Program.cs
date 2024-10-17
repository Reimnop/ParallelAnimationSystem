using System.CommandLine;
using ParallelAnimationSystem.FFmpeg;

var beatmapOption = new Option<string>(
    aliases: ["-b", "--beatmap"],
    description: "Path to the beatmap file (.lsb or .vgd)"
) 
{
    IsRequired = true
};

var audioOption = new Option<string>(
    aliases: ["-a", "--audio"],
    description: "Path to the audio file (.ogg)"
) 
{
    IsRequired = true
};

var outputOption = new Option<string>(
    aliases: ["-o", "--output"],
    description: "Path to the output file"
)
{
    IsRequired = true
};

var widthOption = new Option<int>(
    aliases: ["--width"],
    description: "Width of the output video"
);

var heightOption = new Option<int>(
    aliases: ["--height"],
    description: "Height of the output video"
);

var framerateOption = new Option<int>(
    aliases: ["--framerate"],
    description: "Framerate of the output video",
    getDefaultValue: () => 60
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
rootCommand.AddOption(outputOption);
rootCommand.AddOption(widthOption);
rootCommand.AddOption(heightOption);
rootCommand.AddOption(framerateOption);
rootCommand.AddOption(seedOption);
rootCommand.AddOption(speedOption);
rootCommand.AddOption(backendOption);
rootCommand.AddOption(postProcessingOption);
rootCommand.AddOption(textRenderingOption);

rootCommand.SetHandler(context =>
{
    var beatmapPath = context.ParseResult.GetValueForOption(beatmapOption)!;
    var audioPath = context.ParseResult.GetValueForOption(audioOption)!;
    var outputPath = context.ParseResult.GetValueForOption(outputOption)!;
    var sizeX = context.ParseResult.GetValueForOption(widthOption);
    var sizeY = context.ParseResult.GetValueForOption(heightOption);
    var framerate = context.ParseResult.GetValueForOption(framerateOption);
    var seed = context.ParseResult.GetValueForOption(seedOption);
    var speed = context.ParseResult.GetValueForOption(speedOption);
    var backend = context.ParseResult.GetValueForOption(backendOption);
    var enablePostProcessing = context.ParseResult.GetValueForOption(postProcessingOption);
    var enableTextRendering = context.ParseResult.GetValueForOption(textRenderingOption);
    
    FFmpegStartup.ConsumeOptions(
        beatmapPath,
        audioPath,
        outputPath,
        sizeX,
        sizeY,
        framerate,
        seed,
        speed,
        backend,
        enablePostProcessing,
        enableTextRendering
    );
});

return rootCommand.Invoke(args);