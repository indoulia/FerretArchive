using Ferret.Cli.Infrastructure;
using Ferret.Cli.Modules;
using Ferret.Core.Primitives;
using Ferret.Core.Runtime;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Cli.Tests.Modules;

public sealed class DiagnosticsModuleTests
{
    [Fact]
    public void Metadata_Id() => Assert.Equal("ferret.diagnostics", CreateModule().Metadata.Id);

    [Fact]
    public void Metadata_Name() => Assert.Equal("Ferret Diagnostics", CreateModule().Metadata.Name);

    [Fact]
    public void Metadata_Version_MatchesPlatform()
    {
        var versionWithoutMetadata = StripBuildMetadata(FerretPlatform.Version);
        Assert.Equal(SemanticVersion.Parse(versionWithoutMetadata), CreateModule().Metadata.Version);
    }

    [Fact]
    public async Task OnStartingAsync_Completes() =>
        await CreateModule().OnStartingAsync(new FakeModuleCtx(), CancellationToken.None);

    [Fact]
    public async Task OnStartedAsync_Completes() =>
        await CreateModule().OnStartedAsync(new FakeModuleCtx(), CancellationToken.None);

    [Fact]
    public async Task OnStoppedAsync_Completes() =>
        await CreateModule().OnStoppedAsync(new FakeModuleCtx(), CancellationToken.None);

    private static DiagnosticsModule CreateModule() => new(NullLogger<DiagnosticsModule>.Instance);

    private static string StripBuildMetadata(string version)
    {
        var plusIndex = version.IndexOf('+', StringComparison.Ordinal);
        return plusIndex >= 0 ? version[..plusIndex] : version;
    }
}
