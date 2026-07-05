using Ferret.Core.Connectors;

namespace Ferret.Connectors.Filesystem.Ignore;

/// <summary>Applies .ferretignore patterns (same format as .gitignore). Returns false for non-filesystem URIs.</summary>
public sealed class FerretIgnoreProvider : IIgnoreProvider
{
    private readonly IReadOnlyList<string> _patterns;

    /// <summary>Initializes a new instance of the <see cref="FerretIgnoreProvider"/> class.</summary>
    /// <param name="rootPath">The root directory path.</param>
    public FerretIgnoreProvider(string rootPath)
    {
        ArgumentNullException.ThrowIfNull(rootPath);
        var ferretIgnore = Path.Join(rootPath, ".ferretignore");
        _patterns = File.Exists(ferretIgnore)
            ? File.ReadAllLines(ferretIgnore)
                  .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith('#'))
                  .Select(l => l.Trim())
                  .ToList()
            : [];
    }

    /// <inheritdoc/>
    public bool ShouldIgnore(AssetDescriptor asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        if (!string.Equals(asset.CanonicalUri.Scheme, "filesystem", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var path = Uri.UnescapeDataString(asset.CanonicalUri.AbsolutePath).TrimStart('/');
        var name = Path.GetFileName(path);

        return _patterns.Any(pattern =>
            GitIgnoreProvider.MatchesPattern(pattern, path) || GitIgnoreProvider.MatchesPattern(pattern, name));
    }
}
