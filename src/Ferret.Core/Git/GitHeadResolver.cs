namespace Ferret.Core.Git;

/// <summary>
/// Resolves the current git HEAD commit SHA for a workspace directly from the on-disk
/// <c>.git</c> directory (loose refs and <c>packed-refs</c>), without shelling out to git.
/// </summary>
public static class GitHeadResolver
{
    /// <summary>Attempts to resolve the current HEAD commit SHA for the repository rooted at <paramref name="workspaceRoot"/>.</summary>
    /// <param name="workspaceRoot">Absolute path to the workspace root.</param>
    /// <returns>The resolved commit SHA, or null if the workspace is not a git repository or the ref could not be resolved.</returns>
    public static string? TryResolveHeadSha(string workspaceRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);

        var headPath = Path.Join(workspaceRoot, ".git", "HEAD");
        if (!TryReadAllText(headPath, out var headContent))
        {
            return null;
        }

        headContent = headContent.Trim();

        if (!headContent.StartsWith("ref:", StringComparison.Ordinal))
        {
            // Detached HEAD: the file itself already contains a raw SHA.
            return IsLikelySha(headContent) ? headContent : null;
        }

        var refName = headContent["ref:".Length..].Trim();
        var refPath = Path.Join(workspaceRoot, ".git", refName.Replace('/', Path.DirectorySeparatorChar));
        if (TryReadAllText(refPath, out var loose))
        {
            var sha = loose.Trim();
            return IsLikelySha(sha) ? sha : null;
        }

        // Loose ref file doesn't exist -- the branch may be packed. Fall back to packed-refs.
        return TryResolveFromPackedRefs(workspaceRoot, refName);
    }

    private static string? TryResolveFromPackedRefs(string workspaceRoot, string refName)
    {
        var packedRefsPath = Path.Join(workspaceRoot, ".git", "packed-refs");
        if (!File.Exists(packedRefsPath))
        {
            return null;
        }

        try
        {
            foreach (var line in File.ReadLines(packedRefsPath))
            {
                if (line.Length == 0 || line[0] is '#' or '^')
                {
                    continue;
                }

                var parts = line.Split(' ', 2);
                if (parts.Length == 2 && string.Equals(parts[1].Trim(), refName, StringComparison.Ordinal))
                {
                    return IsLikelySha(parts[0]) ? parts[0] : null;
                }
            }
        }
        catch (IOException)
        {
            return null;
        }

        return null;
    }

    private static bool TryReadAllText(string path, out string content)
    {
        if (!File.Exists(path))
        {
            content = string.Empty;
            return false;
        }

        try
        {
            content = File.ReadAllText(path);
            return true;
        }
        catch (IOException)
        {
            content = string.Empty;
            return false;
        }
    }

    private static bool IsLikelySha(string value) =>
        value.Length is >= 7 and <= 40 && value.All(Uri.IsHexDigit);
}
