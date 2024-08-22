using System.Diagnostics.CodeAnalysis;
using CommandLine;
using OpenTK.Graphics.OpenGL4;
using ParallelAnimationSystem;

public class Program
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Options))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(GL))]
    public static async Task Main(string[] args)
    {
        await Parser.Default
            .ParseArguments<Options>(args)
            .WithParsedAsync(Startup.StartAppAsync);
    }
}
