using CommandLine;
using ParallelAnimationSystem;

await Parser.Default.ParseArguments<Options>(args)
    .WithParsedAsync(async o =>
    {
        await Startup.StartAppAsync(o.LevelPath, o.AudioPath, o.Format);
    });
