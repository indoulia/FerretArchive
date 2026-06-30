using Ferret.Core.Primitives;

namespace Ferret.Core.Runtime;

/// <summary>Describes a platform module's identity and capabilities.</summary>
public sealed class ModuleMetadata : IEquatable<ModuleMetadata>
{
    private ModuleMetadata(
        string id,
        string name,
        SemanticVersion version,
        IReadOnlyCollection<ModuleCapability> capabilities,
        string description,
        string author)
    {
        Id = id;
        Name = name;
        Version = version;
        Capabilities = capabilities;
        Description = description;
        Author = author;
    }

    /// <summary>Gets the unique module identifier (e.g. "workspace").</summary>
    public string Id { get; }

    /// <summary>Gets the human-readable module name.</summary>
    public string Name { get; }

    /// <summary>Gets the module version.</summary>
    public SemanticVersion Version { get; }

    /// <summary>Gets the capabilities this module declares.</summary>
    public IReadOnlyCollection<ModuleCapability> Capabilities { get; }

    /// <summary>Gets a short description of the module's purpose.</summary>
    public string Description { get; }

    /// <summary>Gets the module author or team name.</summary>
    public string Author { get; }

    /// <summary>Creates a new <see cref="ModuleMetadata"/> instance.</summary>
    /// <param name="id">The module identifier. Must not be blank.</param>
    /// <param name="name">The human-readable name.</param>
    /// <param name="version">The module version.</param>
    /// <param name="capabilities">The capabilities this module declares.</param>
    /// <param name="description">A short description of the module.</param>
    /// <param name="author">The author or team name.</param>
    /// <returns>A new <see cref="ModuleMetadata"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is blank.</exception>
    public static ModuleMetadata Create(
        string id,
        string name,
        SemanticVersion version,
        IEnumerable<ModuleCapability> capabilities,
        string description,
        string author)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Module ID must not be blank.", nameof(id));
        }

        return new ModuleMetadata(
            id,
            name ?? string.Empty,
            version,
            capabilities?.ToList().AsReadOnly() ?? new List<ModuleCapability>().AsReadOnly(),
            description ?? string.Empty,
            author ?? string.Empty);
    }

    /// <inheritdoc />
    public bool Equals(ModuleMetadata? other)
    {
        if (other is null)
        {
            return false;
        }

        return Id == other.Id && Version.Equals(other.Version);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ModuleMetadata);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Id, Version);

    /// <summary>Returns the module identifier and version as a string.</summary>
    /// <returns>A string representation of the module metadata.</returns>
    public override string ToString() => $"{Id} v{Version}";
}
