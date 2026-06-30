namespace Ferret.Core.Documents;

/// <summary>Describes a specific capability a content parser can provide.
/// Use <see cref="ParserCapabilities"/> for well-known singletons.</summary>
/// <param name="Id">Unique capability identifier (e.g. "plain-text").</param>
/// <param name="Name">Human-readable capability name.</param>
/// <param name="Version">Semantic version of this capability.</param>
/// <param name="Description">Short description for CLI display.</param>
public sealed record ParserCapability(string Id, string Name, string Version, string Description);
