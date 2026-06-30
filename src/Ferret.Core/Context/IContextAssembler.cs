namespace Ferret.Core.Context;

/// <summary>
/// Orchestrates the context assembly pipeline: search → expand → deduplicate → token-budget → package.
/// The default implementation lives in <c>Ferret.AI.ContextAssembler</c>.
/// </summary>
public interface IContextAssembler
{
    /// <summary>
    /// Assembles a context package for the given request.
    /// </summary>
    /// <param name="request">The context assembly parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="ContextPackage"/> with deduplicated, token-budgeted documents.</returns>
    Task<ContextPackage> AssembleAsync(ContextRequest request, CancellationToken ct);
}
