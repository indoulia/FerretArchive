using System.CommandLine;

namespace Ferret.Cli.Commands;

/// <summary>
/// Why: Thin wrapper around RootCommand that exposes InvokeAsync(string[]) without leaking
///      System.CommandLine types into Program.cs or tests. InvocationConfiguration wires the
///      TextWriter override used in tests.
/// Thread Safety: Single Thread Only — one invocation per process.
/// </summary>
internal sealed class FerretApp
{
    private readonly RootCommand _root;
    private readonly TextWriter? _output;

    internal FerretApp(RootCommand root, TextWriter? output)
    {
        _root = root;
        _output = output;
    }

    /// <summary>Parses and invokes the CLI with the given arguments.</summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The process exit code.</returns>
    internal Task<int> InvokeAsync(string[] args)
    {
        var parseResult = _root.Parse(args);
        var config = new InvocationConfiguration
        {
            Output = _output ?? Console.Out,
            Error = Console.Error,
        };
        return parseResult.InvokeAsync(config);
    }
}
