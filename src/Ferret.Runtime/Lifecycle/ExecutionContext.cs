using Ferret.Core.Primitives;
using Ferret.Core.Runtime;

namespace Ferret.Runtime.Lifecycle;

/// <summary>
/// Default implementation of <see cref="IExecutionContext"/>, carrying shared context for a single runtime operation.
/// <para>Why: Provides lifecycle methods a uniform way to access shared runtime services without tight coupling to RuntimeHost.</para>
/// <para>Lifecycle: Created per-operation by LifecycleOrchestrator; not reused across operations.</para>
/// <para>Layer: Ferret.Runtime internal — never exposed publicly; passed through IModuleContext.</para>
/// <para>Thread Safety: Single Thread Only — created and consumed on the same call stack.</para>
/// </summary>
internal sealed class ExecutionContext : IExecutionContext
{
    /// <summary>Initializes a new instance of the <see cref="ExecutionContext"/> class.</summary>
    /// <param name="correlationId">The correlation identifier propagated from the triggering CLI invocation or MCP call.</param>
    /// <param name="executionId">The unique identifier for this execution instance.</param>
    /// <param name="cancellationToken">A token that signals the operation should be cancelled.</param>
    internal ExecutionContext(CorrelationId correlationId, ExecutionId executionId, CancellationToken cancellationToken)
    {
        CorrelationId = correlationId;
        ExecutionId = executionId;
        CancellationToken = cancellationToken;
    }

    /// <inheritdoc/>
    public CorrelationId CorrelationId { get; }

    /// <inheritdoc/>
    public ExecutionId ExecutionId { get; }

    /// <inheritdoc/>
    public CancellationToken CancellationToken { get; }
}
