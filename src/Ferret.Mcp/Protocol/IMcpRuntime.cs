namespace Ferret.Mcp.Protocol;

/// <summary>Entry point that starts the MCP runtime and serves until cancelled.</summary>
public interface IMcpRuntime
{
    /// <summary>Runs the MCP runtime until <paramref name="ct"/> is cancelled.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the runtime shuts down.</returns>
    Task RunAsync(CancellationToken ct);
}
