using System.Diagnostics.CodeAnalysis;

namespace Ferret.Workspace.Graph;

/// <summary>
/// Canonicalizes a git remote URL to a stable identity string per ADR-0026's Identity Rules:
/// <c>git@host:path</c> (SSH shorthand), <c>ssh://[user@]host/path</c>, and <c>https://host/path</c>
/// must all resolve to the same identity for the same underlying repository. Canonical form is
/// <c>host/path</c>, lowercase host, no scheme, no trailing <c>.git</c>, no leading/trailing slash.
/// </summary>
public static class RemoteUrlCanonicalizer
{
    /// <summary>Canonicalizes a raw remote URL.</summary>
    /// <param name="rawUrl">The remote URL as found in git config (any of the supported forms).</param>
    /// <returns>The canonical identity string.</returns>
    [SuppressMessage("Design", "CA1054:Uri parameters should not be strings", Justification = "The SCP-like shorthand form (git@host:path) this method must also accept is not a valid System.Uri.")]
    public static string Canonicalize(string rawUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawUrl);
        var trimmed = rawUrl.Trim();

        if (trimmed.Contains("://", StringComparison.Ordinal))
        {
            var uri = new Uri(trimmed);
            var path = StripGitSuffix(uri.AbsolutePath.Trim('/'));
            return $"{LowercaseHost(uri.Host)}/{path}";
        }

        // SCP-like shorthand: [user@]host:path (e.g. git@github.com:acme/service-a.git).
        var atIndex = trimmed.IndexOf('@', StringComparison.Ordinal);
        var afterUser = atIndex >= 0 ? trimmed[(atIndex + 1)..] : trimmed;
        var colonIndex = afterUser.IndexOf(':', StringComparison.Ordinal);
        if (colonIndex < 0)
        {
            // Not a recognized URL shape — nothing left to canonicalize beyond the .git suffix.
            return StripGitSuffix(trimmed);
        }

        var host = afterUser[..colonIndex];
        var scpPath = StripGitSuffix(afterUser[(colonIndex + 1)..].Trim('/'));
        return $"{LowercaseHost(host)}/{scpPath}";
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Hostnames are conventionally rendered lowercase; this is an identity/display normalization, not a security-sensitive comparison.")]
    private static string LowercaseHost(string host) => host.ToLowerInvariant();

    private static string StripGitSuffix(string value) =>
        value.EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? value[..^4] : value;
}
