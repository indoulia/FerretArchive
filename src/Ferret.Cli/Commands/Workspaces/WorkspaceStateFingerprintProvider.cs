using System.Security.Cryptography;
using System.Text;

using Ferret.Connectors.Filesystem;
using Ferret.Core.Connectors;
using Ferret.Knowledge.Federation;
using Ferret.ParserPlatform;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Commands.Workspaces;

/// <summary>
/// Concrete <see cref="IWorkspaceStateFingerprintProvider"/> for the CLI composition root.
/// Reuses <see cref="FilesystemConnector"/> — the same asset discovery already used by indexing —
/// to enumerate a member repo's files by their stable, workspace-relative <see cref="AssetDescriptor.CanonicalUri"/>,
/// then hashes actual file content (never mtime or absolute paths, per the ADR-0027 Amendment's
/// portability invariant) with SHA-256.
/// </summary>
internal sealed class WorkspaceStateFingerprintProvider : IWorkspaceStateFingerprintProvider
{
    /// <inheritdoc/>
    public async Task<string?> ComputeFingerprintAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var repoDigests = new List<string>();
        foreach (var repo in entry.Members.Repos.OrderBy(r => r.Remote, StringComparer.Ordinal))
        {
            if (repo.LocalPath is null || !Directory.Exists(repo.LocalPath))
            {
                return null;
            }

            var digest = await ComputeRepoDigestAsync(repo.LocalPath, ct).ConfigureAwait(false);
            repoDigests.Add($"{repo.Remote}:{digest}");
        }

        return Sha256Hex(string.Join('\n', repoDigests));
    }

    private static async Task<string> ComputeRepoDigestAsync(string repoPath, CancellationToken ct)
    {
        var connector = new FilesystemConnector(new FilesystemConnectorConfiguration { RootPath = repoPath }, new MimeTypeResolver());
        var fileDigests = new List<(string Uri, string Hash)>();

        await foreach (var descriptor in connector.DiscoverAsync(new AssetDiscoveryOptions(), ct).ConfigureAwait(false))
        {
            if (descriptor.Kind != AssetKind.File)
            {
                continue;
            }

            var stream = await connector.OpenAsync(descriptor, ct).ConfigureAwait(false);
            await using (stream.ConfigureAwait(false))
            {
                var hashBytes = await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false);
                fileDigests.Add((descriptor.CanonicalUri.ToString(), Convert.ToHexStringLower(hashBytes)));
            }
        }

        var combined = string.Join('\n', fileDigests
            .OrderBy(f => f.Uri, StringComparer.Ordinal)
            .Select(f => $"{f.Uri}:{f.Hash}"));
        return Sha256Hex(combined);
    }

    private static string Sha256Hex(string input) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
}
