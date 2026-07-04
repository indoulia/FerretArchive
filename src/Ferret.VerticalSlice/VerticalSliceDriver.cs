using Ferret.Connectors.Filesystem;
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.ParserPlatform;
using Ferret.ParserPlatform.Parsers;
using Ferret.Persistence;

namespace Ferret.VerticalSlice;

/// <summary>
/// Sprint 1 vertical-slice proof harness: composes the existing, real Connector Platform
/// (<see cref="FilesystemConnector"/>) and Parser Platform (<see cref="ParserDispatcher"/>) with
/// the new persistence and resolution pieces (T1–T5) to scan one file, parse it, build a
/// <see cref="DependencyRecord"/>, persist it, and later resolve against it — realizing
/// Milestones 1–5 of the vertical slice plan. Proof-of-concept only, not a CLI-reachable
/// feature; deliberately kept out of production `src/` until a later milestone decides how (or
/// if) this composition is exposed. Public so a separate process (<c>Ferret.VerticalSliceHost</c>)
/// can exercise it across a genuine process boundary, per the plan's Global Constraints.
/// S2-5: <see cref="ScanAndPersistAsync"/> also captures the parser's and connector's own
/// already-existing registration identity (<c>PlainTextParser.Descriptor</c>,
/// <c>FilesystemConnector.Metadata</c>) into the record's <see cref="ConfigurationDependency"/> —
/// it reads, but never redefines, that identity, per ARCH-023's Data Ownership principle.
/// </summary>
public static class VerticalSliceDriver
{
    /// <summary>The fixed engine responsibility Sprint 1's one-shape request identity uses (ARCH-028 §2, property 1).</summary>
    internal const string EngineResponsibility = "ParseFile";

    /// <summary>Scans one named file under <paramref name="rootPath"/>, parses it, and persists the resulting <see cref="DependencyRecord"/>.</summary>
    /// <param name="rootPath">The directory to scan.</param>
    /// <param name="fileName">The display name of the one file to scan, parse, and persist.</param>
    /// <param name="store">The store to persist the resulting record through.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The persisted <see cref="DependencyRecord"/>.</returns>
    public static async Task<DependencyRecord> ScanAndPersistAsync(
        string rootPath,
        string fileName,
        IDependencyStateStore store,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(store);

        var descriptor = await FindDescriptorAsync(rootPath, fileName, ct).ConfigureAwait(false);
        var connector = new FilesystemConnector(
            new FilesystemConnectorConfiguration { RootPath = rootPath },
            new MimeTypeResolver());
        var parser = new PlainTextParser();
        var dispatcher = new ParserDispatcher(ParserRegistryBuilder.Build([parser]));

        ParseResult<Document> result;
        var stream = await connector.OpenAsync(descriptor, ct).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            result = await dispatcher.DispatchAsync(stream, descriptor, ct).ConfigureAwait(false);
        }

        if (result.Kind != ParseResultKind.Success || result.Value is null)
        {
            throw new InvalidOperationException($"Parsing failed for '{fileName}': {result.Kind}.");
        }

        var document = result.Value;
        if (document.SourceFingerprint is null)
        {
            throw new InvalidOperationException($"Parser produced no source fingerprint for '{fileName}'.");
        }

        var record = new DependencyRecord
        {
            EngineResponsibility = EngineResponsibility,
            RequestPath = Path.Join(rootPath, fileName),
            SourceFingerprint = document.SourceFingerprint,
            PlainText = document.PlainText,
            ConfigurationDependency = new ConfigurationDependency
            {
                Parser = new ComponentRegistrationIdentity
                {
                    Id = parser.Descriptor.Id.Value,
                    Version = parser.Descriptor.Version,
                },
                Connector = new ComponentRegistrationIdentity
                {
                    Id = connector.Metadata.Id,
                    Version = connector.Metadata.Version,
                },
            },
        };

        await store.SetRecordAsync(record, ct).ConfigureAwait(false);
        return record;
    }

    /// <summary>
    /// Realizes Milestone 5 (ARCH-033 §1, §4, §5, §3): re-scans <paramref name="fileName"/> for its
    /// current fingerprint, fetches any persisted candidate for the same request identity, and
    /// compares them to produce one of ARCH-027 §3's three outcomes. Composes T2 (fetch), T4
    /// (equivalence), and T5 (comparison). S2-7: also compares the recorded shape-4 identity
    /// against the parser's and connector's current identity (<see cref="ResolutionCheck.CompareConfiguration"/>)
    /// and evaluates the recorded shape-2 chain by following its references through
    /// <paramref name="store"/> (<see cref="ResolutionCheck.CompareChainAsync"/>), combining all
    /// three per ARCH-029 §6 (<see cref="ResolutionCheck.Combine"/>) — introduces no comparison
    /// logic of its own beyond composing what <see cref="ResolutionCheck"/> already provides.
    /// </summary>
    /// <param name="rootPath">The directory to scan.</param>
    /// <param name="fileName">The display name of the one file to resolve.</param>
    /// <param name="store">The store to fetch a persisted candidate from.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The resolution outcome for this request.</returns>
    public static async Task<ResolutionOutcome> ResolveAndReuseAsync(
        string rootPath,
        string fileName,
        IDependencyStateStore store,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(store);

        var descriptor = await FindDescriptorAsync(rootPath, fileName, ct).ConfigureAwait(false);
        var currentFingerprint = descriptor.Fingerprint
            ?? throw new InvalidOperationException($"No fingerprint computed for '{fileName}'.");
        var requestPath = Path.Join(rootPath, fileName);

        // S2-8: corruption/unreadability classification (malformed content, I/O failure) is now
        // FileDependencyStateStore's own responsibility (ARCH-032 §5) — it never throws for an
        // unreadable record, only returns null, so this caller reasons purely in terms of the
        // IDependencyStateStore abstraction, not a storage-technology exception type.
        var record = await store.GetRecordAsync(EngineResponsibility, requestPath, ct).ConfigureAwait(false);
        var recordReadable = record is not null;

        if (record is not null &&
            !RequestEquivalence.AreEquivalent(record.EngineResponsibility, record.RequestPath, EngineResponsibility, requestPath))
        {
            // T2/T3's own contract already guarantees a fetched record matches the queried identity;
            // this only fires if that contract is ever violated, so it fails loudly rather than proceeding.
            throw new InvalidOperationException("Fetched record's identity does not match the request that located it.");
        }

        var sourceContentOutcome = ResolutionCheck.Compare(recordReadable, record?.SourceFingerprint, currentFingerprint);

        if (!recordReadable || record is null)
        {
            // No readable record to compare shape 4/shape 2 against either — the fail-closed
            // shape-1 outcome (Indeterminate) already reflects that; nothing further to combine.
            return sourceContentOutcome;
        }

        var configurationOutcome = ResolutionCheck.CompareConfiguration(record.ConfigurationDependency, CurrentConfigurationDependency(rootPath));
        var chainOutcome = await ResolutionCheck.CompareChainAsync(record.DependencyChain, store, ct).ConfigureAwait(false);

        return ResolutionCheck.Combine([sourceContentOutcome, configurationOutcome, chainOutcome]);
    }

    /// <summary>The parser's and connector's current registration identity — the same values <see cref="ScanAndPersistAsync"/> records, computed the same way, for comparison against a recorded <see cref="ConfigurationDependency"/>.</summary>
    private static ConfigurationDependency CurrentConfigurationDependency(string rootPath)
    {
        var parser = new PlainTextParser();
        var connector = new FilesystemConnector(new FilesystemConnectorConfiguration { RootPath = rootPath }, new MimeTypeResolver());
        return new ConfigurationDependency
        {
            Parser = new ComponentRegistrationIdentity { Id = parser.Descriptor.Id.Value, Version = parser.Descriptor.Version },
            Connector = new ComponentRegistrationIdentity { Id = connector.Metadata.Id, Version = connector.Metadata.Version },
        };
    }

    private static async Task<AssetDescriptor> FindDescriptorAsync(string rootPath, string fileName, CancellationToken ct)
    {
        var connector = new FilesystemConnector(
            new FilesystemConnectorConfiguration { RootPath = rootPath },
            new MimeTypeResolver());

        await foreach (var candidate in connector.DiscoverAsync(AssetDiscoveryOptions.Default, ct).ConfigureAwait(false))
        {
            if (candidate.Kind == AssetKind.File && candidate.DisplayName == fileName)
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"No file named '{fileName}' found under '{rootPath}'.");
    }
}
