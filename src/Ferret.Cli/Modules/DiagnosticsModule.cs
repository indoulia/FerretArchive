using System.Diagnostics.CodeAnalysis;

using Ferret.Cli.Infrastructure;
using Ferret.Core.Primitives;
using Ferret.Core.Runtime;
using Ferret.Runtime.Modules;
using Microsoft.Extensions.Logging;

namespace Ferret.Cli.Modules;

/// <summary>
/// Why: First built-in module — proves the hosting pipeline works end-to-end.
///      Version derived from FerretPlatform.Version so module and CLI versions stay in sync.
/// Lifecycle: Instantiated by StartCommandHandler and RuntimeLifecycleCheck.
/// Thread Safety: Thread Compatible.
/// </summary>
internal sealed class DiagnosticsModule : DefaultModule
{
    private readonly ILogger<DiagnosticsModule> _logger;

    internal DiagnosticsModule(ILogger<DiagnosticsModule> logger)
        : base(ModuleMetadata.Create(
            id: "ferret.diagnostics",
            name: "Ferret Diagnostics",
            version: SemanticVersion.Parse(StripBuildMetadata(FerretPlatform.Version)),
            capabilities: [],
            description: "Built-in diagnostics module — verifies platform startup.",
            author: "Ferret Platform"))
    {
        _logger = logger;
    }

    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "One-time startup logging; not performance-critical")]
    public override Task OnStartingAsync(IModuleContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DiagnosticsModule starting (v{Version})", Metadata.Version);
        return Task.CompletedTask;
    }

    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "One-time startup logging; not performance-critical")]
    public override Task OnStartedAsync(IModuleContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DiagnosticsModule activated.");
        return Task.CompletedTask;
    }

    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "One-time shutdown logging; not performance-critical")]
    public override Task OnStoppedAsync(IModuleContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DiagnosticsModule stopped.");
        return Task.CompletedTask;
    }

    private static string StripBuildMetadata(string version)
    {
        var plusIndex = version.IndexOf('+', StringComparison.Ordinal);
        return plusIndex >= 0 ? version[..plusIndex] : version;
    }
}
