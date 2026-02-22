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
    description: "Width of the output video",
    getDefaultValue: () => 1920
);

var heightOption = new Option<int>(
    aliases: ["--height"],
    description: "Height of the output video",
    getDefaultValue: () => 1080
);

var useEglOption = new Option<bool>(
    aliases: ["--use-egl"],
    description: "Use EGL for context creation",
    getDefaultValue: () => false
);

var framerateOption = new Option<int>(
    aliases: ["--framerate"],
    description: "Framerate of the output video",
    getDefaultValue: () => 60
);

var videoCodecOption = new Option<string>(
    aliases: ["--video-codec"],
    description: "Video codec to use",
    getDefaultValue: () => "libx264"
);

var audioCodecOption = new Option<string>(
    aliases: ["--audio-codec"],
    description: "Audio codec to use",
    getDefaultValue: () => "aac"
);

var seedOption = new Option<ulong?>(
    aliases: ["--seed"],
    description: "Seed for the random number generator",
    getDefaultValue: () => null
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
rootCommand.AddOption(useEglOption);
rootCommand.AddOption(framerateOption);
rootCommand.AddOption(videoCodecOption);
rootCommand.AddOption(audioCodecOption);
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
    var useEgl = context.ParseResult.GetValueForOption(useEglOption);
    var framerate = context.ParseResult.GetValueForOption(framerateOption);
    var videoCodec = context.ParseResult.GetValueForOption(videoCodecOption)!;
    var audioCodec = context.ParseResult.GetValueForOption(audioCodecOption)!;
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
        useEgl,
        framerate,
        videoCodec,
        audioCodec,
        seed,
        speed,
        backend,
        enablePostProcessing,
        enableTextRendering
    );
});

return rootCommand.Invoke(args);