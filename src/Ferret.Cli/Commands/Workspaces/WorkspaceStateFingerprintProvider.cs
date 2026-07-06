using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Ferret.Connectors.Filesystem;
using Ferret.Core.Connectors;
using Ferret.Core.Indexing;
using Ferret.Core.Workspace;
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
/// <remarks>
/// Registered as a singleton (<c>WorkspacesCliModule</c>), so <see cref="_cache"/> lives for the
/// process lifetime. Every call still re-derives the fingerprint on demand (ADR-0027 Amendment
/// invariant #1) — the cache only skips the expensive per-file content read+hash when a cheap,
/// I/O-free metadata scan (path + size + mtime, mirroring the same heuristic already used by
/// <c>IndexPipeline</c>'s incremental fingerprint) proves the repo's directory listing hasn't moved
/// since the last computation. Metadata is never itself the fingerprint — it only gates reuse of a
/// previously-computed, content-based digest for the *same local path*, so it can't affect
/// cross-checkout portability (a different checkout path is always a cache miss and gets a fresh
/// content hash).
/// </remarks>
internal sealed class WorkspaceStateFingerprintProvider : IWorkspaceStateFingerprintProvider
{
    private readonly ConcurrentDictionary<string, (string MetadataSignature, string ContentDigest)> _cache = new(StringComparer.Ordinal);

    /// <summary>Gets the number of times a full per-file content hash was actually performed. Test-only observability for the metadata-gated cache.</summary>
    internal int ContentDigestComputationCount { get; private set; }

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

    /// <inheritdoc/>
    public Task<string?> ComputeIndexChangeSignalAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var signals = new List<string>();
        foreach (var repo in entry.Members.Repos.OrderBy(r => r.Remote, StringComparer.Ordinal))
        {
            if (repo.LocalPath is null)
            {
                return Task.FromResult<string?>(null);
            }

            var indexPath = Path.Join(
                repo.LocalPath,
                WorkspaceLayout.RootDirectoryName,
                IndexLayout.IndexDirectoryName,
                IndexLayout.KeywordDirectoryName,
                IndexLayout.KeywordDatabaseFileName);

            var info = new FileInfo(indexPath);
            if (!info.Exists)
            {
                // Fail closed: no index built yet means we cannot say whether searchable content
                // changed, the same disposition as an unreachable checkout for the real fingerprint.
                return Task.FromResult<string?>(null);
            }

            signals.Add($"{repo.Remote}:{info.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture)}:{info.Length.ToString(CultureInfo.InvariantCulture)}");
        }

        return Task.FromResult<string?>(string.Join('\n', signals));
    }

    private static string ComputeMetadataSignature(List<AssetDescriptor> descriptors)
    {
        var combined = string.Join('\n', descriptors
            .OrderBy(d => d.CanonicalUri.ToString(), StringComparer.Ordinal)
            .Select(d => $"{d.CanonicalUri}:{d.SizeBytes ?? -1}:{d.LastModified.ToUnixTimeMilliseconds()}"));
        return Sha256Hex(combined);
    }

    private static async Task<string> ComputeContentDigestAsync(
        FilesystemConnector connector, List<AssetDescriptor> descriptors, CancellationToken ct)
    {
        var fileDigests = new List<(string Uri, string Hash)>();

        foreach (var descriptor in descriptors)
        {
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

    private async Task<string> ComputeRepoDigestAsync(string repoPath, CancellationToken ct)
    {
        var connector = new FilesystemConnector(new FilesystemConnectorConfiguration { RootPath = repoPath }, new MimeTypeResolver());
        var descriptors = new List<AssetDescriptor>();

        await foreach (var descriptor in connector.DiscoverAsync(new AssetDiscoveryOptions(), ct).ConfigureAwait(false))
        {
            if (descriptor.Kind == AssetKind.File)
            {
                descriptors.Add(descriptor);
            }
        }

        // Optimization gate only, never the fingerprint itself (not portable across clones) -- any
        // detected change forces recomputation of the content-based fingerprint ADR-0027 requires.
        var metadataSignature = ComputeMetadataSignature(descriptors);
        var cacheKey = Path.GetFullPath(repoPath);
        if (_cache.TryGetValue(cacheKey, out var cached) && cached.MetadataSignature == metadataSignature)
        {
            return cached.ContentDigest;
        }

        var contentDigest = await ComputeContentDigestAsync(connector, descriptors, ct).ConfigureAwait(false);
        ContentDigestComputationCount++;
        _cache[cacheKey] = (metadataSignature, contentDigest);
        return contentDigest;
    }
}
