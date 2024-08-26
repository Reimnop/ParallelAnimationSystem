using System.Diagnostics.CodeAnalysis;
using CommandLine;
using ParallelAnimationSystem;

public class Program
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Options))]
    public static void Main(string[] args)
    {
        Parser.Default
            .ParseArguments<Options>(args)
            .WithParsed(Startup.StartApp);
    }
}
