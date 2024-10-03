using CommandLine;
using ParallelAnimationSystem.Desktop;

Parser.Default
    .ParseArguments<Options>(args)
    .WithParsed(DesktopStartup.ConsumeOptions);