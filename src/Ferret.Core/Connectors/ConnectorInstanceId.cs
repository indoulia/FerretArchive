namespace Ferret.Core.Connectors;

/// <summary>Strongly-typed identifier for a workspace-scoped connector instance (e.g. "src-root").</summary>
/// <param name="Value">The raw string value.</param>
public sealed record ConnectorInstanceId(string Value)
{
    /// <inheritdoc/>
    public override string ToString() => Value;
}
