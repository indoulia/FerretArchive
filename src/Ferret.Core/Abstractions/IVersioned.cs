using Ferret.Core.Primitives;

namespace Ferret.Core.Abstractions;

/// <summary>Marks a component or artifact as carrying a semantic version.</summary>
public interface IVersioned
{
    /// <summary>Gets the semantic version of this component.</summary>
    SemanticVersion Version { get; }
}
