using Ferret.Core.Primitives;

namespace Ferret.Core.Runtime;

/// <summary>Carries the correlation and execution identifiers for a single platform operation.</summary>
public interface IExecutionContext
{
    /// <summary>Gets the correlation identifier propagated from the triggering CLI invocation or MCP call.</summary>
    CorrelationId CorrelationId { get; }

    /// <summary>Gets the unique identifier for this execution instance.</summary>
    ExecutionId ExecutionId { get; }

    /// <summary>Gets a cancellation token that signals the operation should be cancelled.</summary>
    CancellationToken CancellationToken { get; }
}
