using Ferret.Cli.Commands;
using Ferret.VerticalSlice;

using Xunit;

namespace Ferret.Integration.Tests;

/// <summary>
/// Realizes Milestone 6 / T8: proves ARCH-034 §2's indistinguishable-output guarantee against
/// real code — the reuse (Satisfied) path and the recompute (Not-satisfied/Indeterminate) path
/// must write byte-identical content via the real <c>IFerretContext.Services.Output</c>
/// abstraction. This command is deliberately never registered in <c>Ferret.Cli</c>'s real module
/// list (<c>Program.cs</c>) — per ARCH-034 §5, "no new CLI command" is not this document's
/// decision to make, so this stays a test-only exercise of the same handler contract
/// (<see cref="ICommandHandler"/>, <see cref="IFerretContext"/>, <see cref="CommandResult"/>)
/// real commands use, built and invoked entirely from test code via <see cref="RootCommandFactory"/>.
/// </summary>
public sealed class VerticalSliceCommandHandlerTests : IDisposable
{
    private readonly string _rootPath;
    private readonly string _fileName = "sample.txt";
    private readonly string _storePath;

    public VerticalSliceCommandHandlerTests()
    {
        _rootPath = Path.Join(Path.GetTempPath(), $"ferret-cli-vslice-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_rootPath);
        File.WriteAllText(Path.Join(_rootPath, _fileName), "hello vertical slice cli");
        _storePath = Path.Join(_rootPath, ".ferret", "temp", "record.json");
    }

    [Fact]
    public async Task Output_IsByteIdentical_ForRecomputePathAndReusePath()
    {
        var filePath = Path.Join(_rootPath, _fileName);

        using var firstOutput = new StringWriter();
        var firstExitCode = await RunAsync(filePath, _storePath, firstOutput);

        using var secondOutput = new StringWriter();
        var secondExitCode = await RunAsync(filePath, _storePath, secondOutput);

        Assert.Equal(0, firstExitCode);
        Assert.Equal(0, secondExitCode);
        Assert.Equal(firstOutput.ToString(), secondOutput.ToString());
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    private static Task<int> RunAsync(string filePath, string storePath, TextWriter output)
    {
        var app = RootCommandFactory.Build([new CoreCliModule(), new VerticalSliceCliModule(storePath)], output);
        return app.InvokeAsync(["vslice-resolve", filePath]);
    }
}
