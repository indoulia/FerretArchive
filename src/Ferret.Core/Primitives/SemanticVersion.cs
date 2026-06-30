namespace Ferret.Core.Primitives;

/// <summary>Represents a semantic version following the SemVer 2.0.0 specification.</summary>
public sealed class SemanticVersion : IEquatable<SemanticVersion>, IComparable<SemanticVersion>
{
    private SemanticVersion(int major, int minor, int patch, string? preRelease)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = preRelease;
    }

    /// <summary>Gets the major version number.</summary>
    public int Major { get; }

    /// <summary>Gets the minor version number.</summary>
    public int Minor { get; }

    /// <summary>Gets the patch version number.</summary>
    public int Patch { get; }

    /// <summary>Gets the pre-release label, or <see langword="null"/> if this is a stable release.</summary>
    public string? PreRelease { get; }

    /// <summary>Returns a value indicating whether <paramref name="left"/> is less than <paramref name="right"/>.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise <see langword="false"/>.</returns>
    public static bool operator <(SemanticVersion? left, SemanticVersion? right) =>
        left is null ? right is not null : left.CompareTo(right) < 0;

    /// <summary>Returns a value indicating whether <paramref name="left"/> is greater than <paramref name="right"/>.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise <see langword="false"/>.</returns>
    public static bool operator >(SemanticVersion? left, SemanticVersion? right) =>
        left is not null && left.CompareTo(right) > 0;

    /// <summary>Returns a value indicating whether <paramref name="left"/> is less than or equal to <paramref name="right"/>.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise <see langword="false"/>.</returns>
    public static bool operator <=(SemanticVersion? left, SemanticVersion? right) =>
        left is null || left.CompareTo(right) <= 0;

    /// <summary>Returns a value indicating whether <paramref name="left"/> is greater than or equal to <paramref name="right"/>.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise <see langword="false"/>.</returns>
    public static bool operator >=(SemanticVersion? left, SemanticVersion? right) =>
        left is null ? right is null : left.CompareTo(right) >= 0;

    /// <summary>Returns a value indicating whether <paramref name="left"/> is equal to <paramref name="right"/>.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> equals <paramref name="right"/>; otherwise <see langword="false"/>.</returns>
    public static bool operator ==(SemanticVersion? left, SemanticVersion? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Returns a value indicating whether <paramref name="left"/> is not equal to <paramref name="right"/>.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> does not equal <paramref name="right"/>; otherwise <see langword="false"/>.</returns>
    public static bool operator !=(SemanticVersion? left, SemanticVersion? right) =>
        !(left == right);

    /// <summary>Creates a new semantic version from major, minor, and patch version numbers.</summary>
    /// <param name="major">The major version number.</param>
    /// <param name="minor">The minor version number.</param>
    /// <param name="patch">The patch version number.</param>
    /// <param name="preRelease">The pre-release label, or <see langword="null"/> for a stable release.</param>
    /// <returns>A new <see cref="SemanticVersion"/> instance.</returns>
    public static SemanticVersion Create(int major, int minor, int patch, string? preRelease = null)
    {
        return new SemanticVersion(major, minor, patch, preRelease);
    }

    /// <summary>Parses a semantic version string in the form <c>MAJOR.MINOR.PATCH[-pre-release]</c>.</summary>
    /// <param name="value">The version string to parse.</param>
    /// <returns>A parsed <see cref="SemanticVersion"/> instance.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="value"/> is not a valid semantic version.</exception>
    public static SemanticVersion Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var dashIndex = value.IndexOf('-', StringComparison.Ordinal);
        var corePart = dashIndex >= 0 ? value[..dashIndex] : value;
        var preRelease = dashIndex >= 0 ? value[(dashIndex + 1)..] : null;

        var segments = corePart.Split('.');
        if (segments.Length != 3 ||
            !int.TryParse(segments[0], out var major) ||
            !int.TryParse(segments[1], out var minor) ||
            !int.TryParse(segments[2], out var patch) ||
            major < 0 || minor < 0 || patch < 0)
        {
            throw new FormatException($"'{value}' is not a valid semantic version. Expected MAJOR.MINOR.PATCH[-pre-release].");
        }

        return new SemanticVersion(major, minor, patch, preRelease);
    }

    /// <inheritdoc/>
    public int CompareTo(SemanticVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        var cmp = Major.CompareTo(other.Major);
        if (cmp != 0)
        {
            return cmp;
        }

        cmp = Minor.CompareTo(other.Minor);
        if (cmp != 0)
        {
            return cmp;
        }

        cmp = Patch.CompareTo(other.Patch);
        if (cmp != 0)
        {
            return cmp;
        }

        // Stable > pre-release (SemVer §11.4)
        if (PreRelease is null && other.PreRelease is not null)
        {
            return 1;
        }

        if (PreRelease is not null && other.PreRelease is null)
        {
            return -1;
        }

        return string.Compare(PreRelease, other.PreRelease, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public bool Equals(SemanticVersion? other) => other is not null && CompareTo(other) == 0;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SemanticVersion other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, PreRelease);

    /// <summary>Returns the version string in <c>MAJOR.MINOR.PATCH[-pre-release]</c> format.</summary>
    /// <returns>The semantic version string.</returns>
    public override string ToString() =>
        PreRelease is null ? $"{Major}.{Minor}.{Patch}" : $"{Major}.{Minor}.{Patch}-{PreRelease}";
}
