using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics;
using ParallelAnimationSystem.Desktop;
using ParallelAnimationSystem.Desktop.FFmpeg;

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

var useEglOption = new Option<bool>(
    aliases: ["--use-egl"],
    description: "Use EGL for context creation",
    getDefaultValue: () => false
);

var seedOption = new Option<ulong?>(
    aliases: ["--seed"],
    description: "Seed for the random number generator",
    getDefaultValue: () => null
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

var ffmpegPathOption = new Option<string>(
    aliases: ["--ffmpeg-path"],
    description: "(FFmpeg) Path to the FFmpeg executable",
    getDefaultValue: () => "ffmpeg"
);

var outputPathOption = new Option<string?>(
    aliases: ["--output-path"],
    description: "(FFmpeg) Path to the output video file, set this to render to video"
);

var enablePreviewOption = new Option<bool>(
    aliases: ["--preview"],
    description: "(FFmpeg) Enable preview window (may reduce rendering performance)",
    getDefaultValue: () => false
);

var rootCommand = new RootCommand("Parallel Animation System");
rootCommand.AddOption(beatmapOption);
rootCommand.AddOption(audioOption);
rootCommand.AddOption(widthOption);
rootCommand.AddOption(heightOption);
rootCommand.AddOption(vsyncOption);
rootCommand.AddOption(useEglOption);
rootCommand.AddOption(seedOption);
rootCommand.AddOption(backendOption);
rootCommand.AddOption(lockAspectOption);
rootCommand.AddOption(postProcessingOption);
rootCommand.AddOption(textRenderingOption);
rootCommand.AddOption(ffmpegPathOption);
rootCommand.AddOption(outputPathOption);
rootCommand.AddOption(enablePreviewOption);

rootCommand.SetHandler(context =>
{
    var beatmapPath = context.ParseResult.GetValueForOption(beatmapOption)!;
    var audioPath = context.ParseResult.GetValueForOption(audioOption)!;
    var width = context.ParseResult.GetValueForOption(widthOption);
    var height = context.ParseResult.GetValueForOption(heightOption);
    var vsync = context.ParseResult.GetValueForOption(vsyncOption);
    var useEgl = context.ParseResult.GetValueForOption(useEglOption);
    var seed = context.ParseResult.GetValueForOption(seedOption);
    var backend = context.ParseResult.GetValueForOption(backendOption);
    var lockAspectRatio = context.ParseResult.GetValueForOption(lockAspectOption);
    var enablePostProcessing = context.ParseResult.GetValueForOption(postProcessingOption);
    var enableTextRendering = context.ParseResult.GetValueForOption(textRenderingOption);
    var ffmpegPath = context.ParseResult.GetValueForOption(ffmpegPathOption);
    var outputPath = context.ParseResult.GetValueForOption(outputPathOption);
    var enablePreview = context.ParseResult.GetValueForOption(enablePreviewOption);

    if (outputPath is null)
    {
        DesktopStartup.ConsumeOptions(
            beatmapPath,
            audioPath,
            width,
            height,
            vsync,
            useEgl,
            seed,
            backend,
            lockAspectRatio,
            enablePostProcessing,
            enableTextRendering);
    }
    else
    {
        Debug.Assert(ffmpegPath is not null);
        
        FFmpegStartup.ConsumeOptions(   
            beatmapPath,
            audioPath,
            width,
            height,
            useEgl,
            seed,
            backend,
            lockAspectRatio,
            enablePostProcessing,
            enableTextRendering,
            ffmpegPath,
            outputPath,
            enablePreview);
    }
});

var parser = new CommandLineBuilder(rootCommand)
    .UseVersionOption()
    .UseHelp()
    .UseEnvironmentVariableDirective()
    .UseParseDirective()
    .UseSuggestDirective()
    .RegisterWithDotnetSuggest()
    .UseTypoCorrections()
    .UseParseErrorReporting()
    // .UseExceptionHandler()
    .CancelOnProcessTermination()
    .Build();
    
return parser.Invoke(args);