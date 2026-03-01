using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
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

void AddCommonOptions(Command command)
{
    command.AddOption(beatmapOption);
    command.AddOption(audioOption);
    command.AddOption(widthOption);
    command.AddOption(heightOption);
    command.AddOption(useEglOption);
    command.AddOption(seedOption);
    command.AddOption(backendOption);
    command.AddOption(postProcessingOption);
    command.AddOption(textRenderingOption);
}

var rootCommand = new RootCommand("Parallel Animation System");

// run subcommand
{
    var vsyncOption = new Option<bool>(
        aliases: ["--vsync"],
        description: "Enable VSync",
        getDefaultValue: () => true
    );

    var lockAspectOption = new Option<bool>(
        aliases: ["--lock-aspect"],
        description: "Lock the aspect ratio to 16:9",
        getDefaultValue: () => true
    );

    var runSubcommand = new Command("run", "Run the beatmap in a window with real-time rendering");
    AddCommonOptions(runSubcommand);
    runSubcommand.AddOption(vsyncOption);
    runSubcommand.AddOption(lockAspectOption);
    
    rootCommand.AddCommand(runSubcommand);
    
    runSubcommand.SetHandler(context =>
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
    });
}

// render subcommand
{
    var ffmpegPathOption = new Option<string>(
        aliases: ["--ffmpeg-path"],
        description: "Path to the FFmpeg executable",
        getDefaultValue: () => "ffmpeg"
    );

    var outputPathOption = new Option<string>(
        aliases: ["-o", "--output-path"],
        description: "Path to the output video file"
    )
    {
        IsRequired = true
    };

    var ffmpegArgsOption = new Option<string>(
        aliases: ["--ffmpeg-args"],
        description: "Output arguments to pass to FFmpeg",
        getDefaultValue: () => "-c:v libx264 -pix_fmt yuv420p -preset slow -c:a aac"
    );

    var enablePreviewOption = new Option<bool>(
        aliases: ["--preview"],
        description: "Enable preview window (may reduce rendering performance)",
        getDefaultValue: () => false
    );
    
    var ffmpegSubcommand = new Command("render", "Render the beatmap to a video file using FFmpeg");
    AddCommonOptions(ffmpegSubcommand);
    ffmpegSubcommand.AddOption(ffmpegPathOption);
    ffmpegSubcommand.AddOption(outputPathOption);
    ffmpegSubcommand.AddOption(ffmpegArgsOption);
    ffmpegSubcommand.AddOption(enablePreviewOption);
    
    rootCommand.AddCommand(ffmpegSubcommand);
    
    ffmpegSubcommand.SetHandler(context =>
    {
        var beatmapPath = context.ParseResult.GetValueForOption(beatmapOption)!;
        var audioPath = context.ParseResult.GetValueForOption(audioOption)!;
        var width = context.ParseResult.GetValueForOption(widthOption);
        var height = context.ParseResult.GetValueForOption(heightOption);
        var useEgl = context.ParseResult.GetValueForOption(useEglOption);
        var seed = context.ParseResult.GetValueForOption(seedOption);
        var backend = context.ParseResult.GetValueForOption(backendOption);
        var enablePostProcessing = context.ParseResult.GetValueForOption(postProcessingOption);
        var enableTextRendering = context.ParseResult.GetValueForOption(textRenderingOption);
        var ffmpegPath = context.ParseResult.GetValueForOption(ffmpegPathOption)!;
        var outputPath = context.ParseResult.GetValueForOption(outputPathOption)!;
        var ffmpegArgs = context.ParseResult.GetValueForOption(ffmpegArgsOption)!;
        var enablePreview = context.ParseResult.GetValueForOption(enablePreviewOption);
        
        FFmpegStartup.ConsumeOptions(   
            beatmapPath,
            audioPath,
            width,
            height,
            useEgl,
            seed,
            backend,
            enablePostProcessing,
            enableTextRendering,
            ffmpegPath,
            outputPath,
            ffmpegArgs,
            enablePreview);
    });
}

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