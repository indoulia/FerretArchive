namespace Ferret.Core.Abstractions;

/// <summary>Provides access to arbitrary string metadata associated with an entity.</summary>
public interface IMetadata
{
    /// <summary>Gets the metadata dictionary for this entity.</summary>
    IReadOnlyDictionary<string, string> Metadata { get; }
}
