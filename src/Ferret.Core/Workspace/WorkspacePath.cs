namespace Ferret.Core.Workspace;

/// <summary>Represents an absolute file system path within or referring to a workspace root.</summary>
public sealed class WorkspacePath : IEquatable<WorkspacePath>
{
    private WorkspacePath(string fullPath)
    {
        FullPath = fullPath;
    }

    /// <summary>Gets the absolute path string.</summary>
    public string FullPath { get; }

    /// <summary>Determines whether two <see cref="WorkspacePath"/> instances are equal.</summary>
    /// <param name="left">The first path to compare.</param>
    /// <param name="right">The second path to compare.</param>
    /// <returns><see langword="true"/> if both paths are equal; otherwise <see langword="false"/>.</returns>
    public static bool operator ==(WorkspacePath? left, WorkspacePath? right)
        => left?.Equals(right) ?? right is null;

    /// <summary>Determines whether two <see cref="WorkspacePath"/> instances are not equal.</summary>
    /// <param name="left">The first path to compare.</param>
    /// <param name="right">The second path to compare.</param>
    /// <returns><see langword="true"/> if the paths are not equal; otherwise <see langword="false"/>.</returns>
    public static bool operator !=(WorkspacePath? left, WorkspacePath? right)
        => !(left == right);

    /// <summary>Creates a new <see cref="WorkspacePath"/> from an absolute path string.</summary>
    /// <param name="path">The absolute path. Must not be blank.</param>
    /// <returns>A new <see cref="WorkspacePath"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null, empty, or whitespace.</exception>
    public static WorkspacePath Create(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Workspace path must not be blank.", nameof(path));
        }

        return new WorkspacePath(path);
    }

    /// <summary>Combines this path with a relative segment and returns a new <see cref="WorkspacePath"/>.</summary>
    /// <param name="relative">The relative path segment to append.</param>
    /// <returns>A new <see cref="WorkspacePath"/> with the combined path.</returns>
    public WorkspacePath Combine(string relative)
    {
        return new WorkspacePath(Path.Join(FullPath, relative));
    }

    /// <summary>Returns <see langword="true"/> if this path is located under <paramref name="parent"/>.</summary>
    /// <param name="parent">The parent path to test against.</param>
    /// <returns><see langword="true"/> if this path is a child of the parent; otherwise <see langword="false"/>.</returns>
    public bool IsUnder(WorkspacePath parent)
    {
        if (parent is null)
        {
            return false;
        }

        var parentNormalised = parent.FullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return FullPath.StartsWith(parentNormalised, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public bool Equals(WorkspacePath? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(FullPath, other.FullPath, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    /// <returns><see langword="true"/> if the given object is a <see cref="WorkspacePath"/> with the same path; otherwise <see langword="false"/>.</returns>
    public override bool Equals(object? obj) => Equals(obj as WorkspacePath);

    /// <inheritdoc/>
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(FullPath);

    /// <summary>Returns the full path string.</summary>
    /// <returns>The absolute path string.</returns>
    public override string ToString() => FullPath;
}
