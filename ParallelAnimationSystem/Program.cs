using System.CommandLine;
using ParallelAnimationSystem;

var levelFileOption = new Option<string?>(
    name: "--level",
    description: "The level file to run. Currently only supports .vgd files.");

var audioFileOption = new Option<string?>(
    name: "--audio",
    description: "The audio file to play. Currently only supports .ogg files.");

var rootCommand = new RootCommand
{
    levelFileOption,
    audioFileOption,
};

rootCommand.Description = "Parallel Animation System";

rootCommand.SetHandler(async (levelFile, audioFile) =>
{
    if (levelFile is null || audioFile is null)
    {
        Console.WriteLine("Please provide both a level file and an audio file.");
        return;
    }

    await Startup.StartAppAsync(levelFile, audioFile);
}, levelFileOption, audioFileOption);

return await rootCommand.InvokeAsync(args);