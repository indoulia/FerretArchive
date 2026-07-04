namespace Ferret.Persistence;

/// <summary>
/// The combination of derived-artifact dependencies (ARCH-025 §3 shape 2) a single artifact
/// depends on — S2-6. An ordered set of <see cref="DependencyReference"/>s, never the referenced
/// artifacts' own dependency state (see <see cref="DependencyReference"/>'s own remarks). This
/// type represents a dependency chain; it decides nothing about whether any reference in it is
/// still satisfied — that is resolution's responsibility (ARCH-032 §8), not persistence's, and is
/// deliberately not implemented here.
/// <para/>
/// Provides its own structural (sequence) equality rather than relying on the default equality a
/// record would otherwise synthesize for an <see cref="IReadOnlyList{T}"/> property — a plain
/// list or array compares by reference, which would make two independently-read copies of the
/// same persisted chain compare unequal even when their content is identical.
/// </summary>
public sealed record DependencyChain
{
    /// <summary>Gets the chain with no derived-artifact dependencies — the default for every artifact until it is shown otherwise.</summary>
    public static readonly DependencyChain Empty = new() { References = [] };

    /// <summary>Gets the ordered set of artifacts this chain's owning artifact depends on.</summary>
    public required IReadOnlyList<DependencyReference> References { get; init; }

    /// <inheritdoc/>
    public bool Equals(DependencyChain? other) =>
        other is not null && References.SequenceEqual(other.References);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        foreach (var reference in References)
        {
            hash.Add(reference);
        }

        return hash.ToHashCode();
    }
}
