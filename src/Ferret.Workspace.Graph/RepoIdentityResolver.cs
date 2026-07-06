using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Ferret.Workspace.Graph;

/// <summary>
/// Resolves a local repo path to the durable identity ADR-0026 requires: the canonicalized
/// <c>origin</c> remote, falling back to the alphabetically-first other remote if there is no
/// <c>origin</c>, falling back to a locally-generated identity persisted in the repo's own
/// <c>.ferret/workspace-identity.json</c> if the repo has no remote at all. This is a WIP-012 (CLI)
/// concern per ADR-0026 — <c>Ferret.Workspace.Graph</c>'s registry and manifest types never run git
/// commands or read git config themselves.
/// </summary>
public static class RepoIdentityResolver
{
    private const string LocalIdentityFileName = "workspace-identity.json";

    private static readonly Regex RemoteSectionPattern = new("""^\[remote "(.+)"\]$""", RegexOptions.Compiled);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Resolves the repo at <paramref name="repoPath"/> to its durable identity.</summary>
    /// <param name="repoPath">Absolute path to the repo's root directory.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The canonicalized remote identity, or a <c>local:&lt;guid&gt;</c> fallback identity for a repo with no remote.</returns>
    /// <exception cref="RepoIdentityResolutionException">The path does not exist, or is not a git repository.</exception>
    public static async Task<string> ResolveAsync(string repoPath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repoPath);

        if (!Directory.Exists(repoPath))
        {
            throw new RepoIdentityResolutionException(repoPath, "the path does not exist");
        }

        var gitConfigPath = Path.Join(repoPath, ".git", "config");
        if (!File.Exists(gitConfigPath))
        {
            throw new RepoIdentityResolutionException(repoPath, "not a git repository (no .git/config found)");
        }

        var configContent = await File.ReadAllTextAsync(gitConfigPath, ct).ConfigureAwait(false);
        var remotes = ParseRemotes(configContent);

        if (remotes.TryGetValue("origin", out var originUrl))
        {
            return RemoteUrlCanonicalizer.Canonicalize(originUrl);
        }

        if (remotes.Count > 0)
        {
            var firstByName = remotes.OrderBy(kv => kv.Key, StringComparer.Ordinal).First();
            return RemoteUrlCanonicalizer.Canonicalize(firstByName.Value);
        }

        return await GetOrCreateLocalIdentityAsync(repoPath, ct).ConfigureAwait(false);
    }

    private static Dictionary<string, string> ParseRemotes(string configContent)
    {
        var remotes = new Dictionary<string, string>(StringComparer.Ordinal);
        string? currentRemote = null;

        foreach (var rawLine in configContent.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                var match = RemoteSectionPattern.Match(line);
                currentRemote = match.Success ? match.Groups[1].Value : null;
                continue;
            }

            if (currentRemote is null || !line.StartsWith("url", StringComparison.Ordinal))
            {
                continue;
            }

            var eq = line.IndexOf('=', StringComparison.Ordinal);
            if (eq >= 0)
            {
                remotes[currentRemote] = line[(eq + 1)..].Trim();
            }
        }

        return remotes;
    }

    private static async Task<string> GetOrCreateLocalIdentityAsync(string repoPath, CancellationToken ct)
    {
        var identityFilePath = Path.Join(repoPath, Ferret.Core.Workspace.WorkspaceLayout.RootDirectoryName, LocalIdentityFileName);
        if (File.Exists(identityFilePath))
        {
            var existingJson = await File.ReadAllTextAsync(identityFilePath, ct).ConfigureAwait(false);
            var existing = JsonSerializer.Deserialize<LocalIdentityFile>(existingJson, SerializerOptions);
            if (existing is not null && existing.LocalIdentityId != Guid.Empty)
            {
                return $"local:{existing.LocalIdentityId:N}";
            }
        }

        var newId = Guid.NewGuid();
        var dir = Path.GetDirectoryName(identityFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(new LocalIdentityFile { LocalIdentityId = newId }, SerializerOptions);
        var tmpPath = identityFilePath + ".tmp";
        await File.WriteAllTextAsync(tmpPath, json, ct).ConfigureAwait(false);
        File.Move(tmpPath, identityFilePath, overwrite: true);
        return $"local:{newId:N}";
    }

    private sealed class LocalIdentityFile
    {
        [JsonPropertyName("localIdentityId")]
        public Guid LocalIdentityId { get; set; }
    }
}
