using Ferret.Cli.Diagnostics.Checks;
using Ferret.Core.Documents;
using Ferret.ParserPlatform.Parsers;

namespace Ferret.Cli.Tests.Diagnostics;

public sealed class InstalledParsersCheckTests
{
    [Fact]
    public async Task Passes_When_Parsers_Registered()
    {
        IReadOnlyList<IContentParser> parsers = [new PlainTextParser(), new MarkdownParser(), new JsonParser()];
        var check = new InstalledParsersCheck(parsers, parserCount: 3, supportedExtensionCount: 60);

        var result = await check.RunAsync(context: null!, CancellationToken.None);

        Assert.True(result.Passed);
    }

    [Fact]
    public async Task Warns_When_No_Parsers_Registered()
    {
        var check = new InstalledParsersCheck([], parserCount: 0, supportedExtensionCount: 0);

        var result = await check.RunAsync(context: null!, CancellationToken.None);

        // A warning is advisory: DiagnosticCheckResult.Passed is (Severity != Fail), so a Warn still
        // "passes" (does not make the workspace unhealthy). The zero-parser case is a warning, not a failure.
        Assert.True(result.Passed);
        Assert.True(result.IsWarning);
    }
}
