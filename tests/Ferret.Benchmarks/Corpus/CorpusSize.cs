namespace Ferret.Benchmarks.Corpus;

/// <summary>Benchmark corpus size tiers, aligned with the Benchmark Suite Spec.</summary>
public enum CorpusSize
{
    /// <summary>~200 files.</summary>
    Small,

    /// <summary>~2,000 files.</summary>
    Medium,

    /// <summary>~15,000 files.</summary>
    Enterprise,
}
