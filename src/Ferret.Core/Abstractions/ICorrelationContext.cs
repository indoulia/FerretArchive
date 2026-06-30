using Ferret.Core.Primitives;

namespace Ferret.Core.Abstractions;

/// <summary>Provides access to the correlation identifier for the current operation scope.</summary>
public interface ICorrelationContext
{
    /// <summary>Gets the correlation identifier for the current operation.</summary>
    CorrelationId CorrelationId { get; }
}
