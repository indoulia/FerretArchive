using Ferret.Core.Connectors;

namespace Ferret.Connectors.Filesystem.Ignore;

/// <summary>Applies root-level .gitignore patterns. Returns false for non-filesystem URIs.</summary>
public sealed class GitIgnoreProvider : IIgnoreProvider
{
    private static readonly string[] DoubleGlobSeparator = ["**"];

    private readonly IReadOnlyList<string> _patterns;

    /// <summary>Initializes a new instance of the <see cref="GitIgnoreProvider"/> class.</summary>
    /// <param name="rootPath">The root directory path.</param>
    public GitIgnoreProvider(string rootPath)
    {
        ArgumentNullException.ThrowIfNull(rootPath);
        var gitignore = Path.Join(rootPath, ".gitignore");
        _patterns = File.Exists(gitignore)
            ? File.ReadAllLines(gitignore)
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

        var path = asset.CanonicalUri.AbsolutePath.TrimStart('/');
        var name = Path.GetFileName(path);

        return _patterns.Any(pattern => MatchesPattern(pattern, path) || MatchesPattern(pattern, name));
    }

    internal static bool MatchesPattern(string pattern, string input)
    {
        // Leading / means anchored to root: strip it and require input starts with the remainder.
        if (pattern.StartsWith('/'))
        {
            var anchored = pattern[1..];
            return input.Equals(anchored, StringComparison.OrdinalIgnoreCase)
                || input.StartsWith(anchored + "/", StringComparison.OrdinalIgnoreCase);
        }

        // ** — replace with segment-aware matching.
        if (pattern.Contains("**", StringComparison.Ordinal))
        {
            return MatchesDoubleGlob(pattern, input);
        }

        if (!pattern.Contains('*', StringComparison.Ordinal))
        {
            return string.Equals(input, pattern, StringComparison.OrdinalIgnoreCase)
                || input.EndsWith("/" + pattern, StringComparison.OrdinalIgnoreCase)
                || input.StartsWith(pattern + "/", StringComparison.OrdinalIgnoreCase);
        }

        var parts = pattern.Split('*');
        var pos = 0;
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0)
            {
                continue;
            }

            var idx = input.IndexOf(parts[i], pos, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                return false;
            }

            if (i == 0 && idx > 0 && !pattern.StartsWith('*'))
            {
                return false;
            }

            pos = idx + parts[i].Length;
        }

        return pattern.EndsWith('*') || pos == input.Length || input[pos..].All(c => c == '/');
    }

    private static bool MatchesDoubleGlob(string pattern, string input)
    {
        // Split on ** boundaries; each non-empty segment between ** must be found in order.
        var segments = pattern.Split(DoubleGlobSeparator, StringSplitOptions.None);

        var pos = 0;
        for (var i = 0; i < segments.Length; i++)
        {
            var seg = segments[i].Trim('/');

            if (seg.Length == 0)
            {
                if (i == segments.Length - 1)
                {
                    // Trailing ** — matches everything remaining.
                    return true;
                }

                // Empty between ** (consecutive **) — continue.
                continue;
            }

            if (i == 0)
            {
                // First segment must match at the start (or ** before it allows any prefix).
                if (!input.StartsWith(seg + "/", StringComparison.OrdinalIgnoreCase)
                    && !input.Equals(seg, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                pos = seg.Length;
                continue;
            }

            // Subsequent segments after **: find the segment anywhere from pos onward.
            var found = false;
            while (pos <= input.Length)
            {
                var remaining = input[pos..];
                if (MatchesSingleGlob(seg, remaining))
                {
                    pos += seg.Length;
                    found = true;
                    break;
                }

                var next = input.IndexOf('/', pos);
                if (next < 0)
                {
                    break;
                }

                pos = next + 1;
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    private static bool MatchesSingleGlob(string pattern, string input)
    {
        // Single * does not cross /; operate on first segment only.
        if (!pattern.Contains('*', StringComparison.Ordinal))
        {
            return input.Equals(pattern, StringComparison.OrdinalIgnoreCase)
                || input.StartsWith(pattern + "/", StringComparison.OrdinalIgnoreCase);
        }

        var segment = input.Contains('/', StringComparison.Ordinal)
            ? input[..input.IndexOf('/', StringComparison.Ordinal)]
            : input;

        var parts = pattern.Split('*');
        var pos = 0;
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0)
            {
                continue;
            }

            var idx = segment.IndexOf(parts[i], pos, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                return false;
            }

            if (i == 0 && idx > 0 && !pattern.StartsWith('*'))
            {
                return false;
            }

            pos = idx + parts[i].Length;
        }

        return pattern.EndsWith('*') || pos == segment.Length;
    }
}
