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
internal sealed partial class DiagnosticsModule : DefaultModule
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

    public override Task OnStartingAsync(IModuleContext context, CancellationToken cancellationToken = default)
    {
        LogStarting(_logger, Metadata.Version.ToString());
        return Task.CompletedTask;
    }

    public override Task OnStartedAsync(IModuleContext context, CancellationToken cancellationToken = default)
    {
        LogStarted(_logger);
        return Task.CompletedTask;
    }

    public override Task OnStoppedAsync(IModuleContext context, CancellationToken cancellationToken = default)
    {
        LogStopped(_logger);
        return Task.CompletedTask;
    }

    private static string StripBuildMetadata(string version)
    {
        var plusIndex = version.IndexOf('+', StringComparison.Ordinal);
        return plusIndex >= 0 ? version[..plusIndex] : version;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "DiagnosticsModule starting (v{Version})")]
    private static partial void LogStarting(ILogger logger, string version);

    [LoggerMessage(Level = LogLevel.Information, Message = "DiagnosticsModule activated.")]
    private static partial void LogStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "DiagnosticsModule stopped.")]
    private static partial void LogStopped(ILogger logger);
}
