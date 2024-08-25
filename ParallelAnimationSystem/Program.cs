using System.Diagnostics.CodeAnalysis;
using CommandLine;
using ParallelAnimationSystem;

public class Program
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Options))]
    public static async Task Main(string[] args)
    {
        await Parser.Default
            .ParseArguments<Options>(args)
            .WithParsedAsync(Startup.StartAppAsync);
    }
}
