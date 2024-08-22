using CommandLine;
using ParallelAnimationSystem;

await Parser.Default
    .ParseArguments<Options>(args)
    .WithParsedAsync(Startup.StartAppAsync);
