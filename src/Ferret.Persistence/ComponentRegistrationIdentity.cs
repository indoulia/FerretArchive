namespace Ferret.Persistence;

/// <summary>
/// The registration identity of one already-registered component (a parser or a connector,
/// per ARCH-032 §2.1's dependency shape 4) at the moment it produced an artifact. Deliberately
/// generic — a plain (id, version) pair — so this type carries no reference to any
/// parser-specific or connector-specific type (<c>ParserId</c>, <c>ConnectorId</c>,
/// <c>ParserDescriptor</c>, <c>ConnectorDescriptor</c>); the owning component (Parser Platform,
/// Connector Platform, per ARCH-026 §3) is the one that knows what those identifiers mean, not
/// this persistence-layer type.
/// </summary>
public sealed record ComponentRegistrationIdentity
{
    /// <summary>Gets the stable identifier of the registered component (e.g. a parser's media type, a connector's type id).</summary>
    public required string Id { get; init; }

    /// <summary>Gets the version string of the registered component at production time.</summary>
    public required string Version { get; init; }
}
