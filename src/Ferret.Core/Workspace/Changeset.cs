namespace Ferret.Core.Workspace;

/// <summary>Represents the set of file changes detected since the last index operation.</summary>
public sealed class Changeset
{
    private Changeset(IReadOnlyList<string> added, IReadOnlyList<string> modified, IReadOnlyList<string> deleted, DateTimeOffset detectedAt)
    {
        Added = added;
        Modified = modified;
        Deleted = deleted;
        DetectedAt = detectedAt;
    }

    /// <summary>Gets the paths of files added since the last index.</summary>
    public IReadOnlyList<string> Added { get; }

    /// <summary>Gets the paths of files modified since the last index.</summary>
    public IReadOnlyList<string> Modified { get; }

    /// <summary>Gets the paths of files deleted since the last index.</summary>
    public IReadOnlyList<string> Deleted { get; }

    /// <summary>Gets the UTC timestamp when this changeset was detected.</summary>
    public DateTimeOffset DetectedAt { get; }

    /// <summary>Gets a value indicating whether there are any changes in this changeset.</summary>
    public bool HasChanges => Added.Count > 0 || Modified.Count > 0 || Deleted.Count > 0;

    /// <summary>Creates a new <see cref="Changeset"/>.</summary>
    /// <param name="added">Added file paths.</param>
    /// <param name="modified">Modified file paths.</param>
    /// <param name="deleted">Deleted file paths.</param>
    /// <param name="detectedAt">When the changeset was detected.</param>
    /// <returns>A new <see cref="Changeset"/> instance.</returns>
    public static Changeset Create(IEnumerable<string> added, IEnumerable<string> modified, IEnumerable<string> deleted, DateTimeOffset detectedAt)
    {
        return new Changeset(
            (added ?? Enumerable.Empty<string>()).ToList().AsReadOnly(),
            (modified ?? Enumerable.Empty<string>()).ToList().AsReadOnly(),
            (deleted ?? Enumerable.Empty<string>()).ToList().AsReadOnly(),
            detectedAt);
    }

    /// <summary>Creates an empty changeset with no changes.</summary>
    /// <param name="detectedAt">When the (empty) changeset was detected.</param>
    /// <returns>An empty <see cref="Changeset"/> with no changes.</returns>
    public static Changeset Empty(DateTimeOffset detectedAt)
        => Create(Enumerable.Empty<string>(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), detectedAt);
}
