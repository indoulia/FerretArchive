namespace Ferret.Core.Connectors;

/// <summary>Strongly-typed identifier for a connector type (e.g. "filesystem").</summary>
/// <param name="Value">The raw string value.</param>
public sealed record ConnectorId(string Value)
{
    /// <inheritdoc/>
    public override string ToString() => Value;
}
