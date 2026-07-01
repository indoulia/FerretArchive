namespace Ferret.Benchmarks.Corpus;

/// <summary>Deterministic description of a generated corpus, serialized to corpus.json for
/// reproducibility and benchmark diagnostics. Contains no timestamps or random identifiers.</summary>
/// <param name="GeneratorVersion">The generator schema/version.</param>
/// <param name="Seed">The RNG seed used.</param>
/// <param name="Size">The corpus size tier name.</param>
/// <param name="DocumentCount">The total number of generated documents.</param>
/// <param name="FormatCounts">Document counts keyed by file extension.</param>
/// <param name="ArchetypeCounts">Document counts keyed by tabular archetype title.</param>
public sealed record CorpusManifest(
    string GeneratorVersion,
    int Seed,
    string Size,
    int DocumentCount,
    IReadOnlyDictionary<string, int> FormatCounts,
    IReadOnlyDictionary<string, int> ArchetypeCounts);
