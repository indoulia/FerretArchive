#pragma warning disable SA1402 // File may only contain a single type
namespace Ferret.Core.Search;

/// <summary>
/// Base type for all nodes in the search query AST.
/// The AST is a canonical platform contract — shared by every search provider.
/// Providers translate AST nodes to backend-specific syntax; the AST is never backend-specific.
/// </summary>
public abstract record SearchExpression;

/// <summary>A single keyword term. Matches documents containing this word.</summary>
/// <param name="Value">The keyword (case-insensitive matching is provider-determined).</param>
public sealed record KeywordExpression(string Value) : SearchExpression;

/// <summary>An exact phrase. Matches documents containing these words adjacent and in order.</summary>
/// <param name="Value">The phrase text, excluding surrounding quotes.</param>
public sealed record PhraseExpression(string Value) : SearchExpression;

/// <summary>A prefix match. Matches documents containing any word starting with this prefix.</summary>
/// <param name="Prefix">The prefix (the trailing <c>*</c> is stripped by the parser).</param>
public sealed record PrefixExpression(string Prefix) : SearchExpression;

/// <summary>
/// Implicit AND of two or more operands. Sprint 10: produced for all multi-term queries.
/// All operands must match for a document to be returned.
/// </summary>
/// <param name="Operands">The child expressions — must contain at least two items.</param>
public sealed record AndExpression(IReadOnlyList<SearchExpression> Operands) : SearchExpression
{
    /// <summary>Gets a hash code for this expression.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = 0;
            foreach (var operand in Operands)
            {
                hashCode = (hashCode * 397) ^ operand.GetHashCode();
            }

            return hashCode;
        }
    }

    /// <summary>Determines whether this expression equals another.</summary>
    /// <param name="other">The expression to compare.</param>
    /// <returns>True if equal.</returns>
    public bool Equals(AndExpression? other)
    {
        if (other is null)
        {
            return false;
        }

        if (Operands.Count != other.Operands.Count)
        {
            return false;
        }

        for (int i = 0; i < Operands.Count; i++)
        {
            if (!Operands[i].Equals(other.Operands[i]))
            {
                return false;
            }
        }

        return true;
    }
}

// ── Reserved expressions — not emitted by the Sprint 10 parser ───────────────

/// <summary>Reserved: OR of two or more operands. Not emitted in Sprint 10.</summary>
/// <param name="Operands">The child expressions.</param>
public sealed record OrExpression(IReadOnlyList<SearchExpression> Operands) : SearchExpression
{
    /// <summary>Gets a hash code for this expression.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = 0;
            foreach (var operand in Operands)
            {
                hashCode = (hashCode * 397) ^ operand.GetHashCode();
            }

            return hashCode;
        }
    }

    /// <summary>Determines whether this expression equals another.</summary>
    /// <param name="other">The expression to compare.</param>
    /// <returns>True if equal.</returns>
    public bool Equals(OrExpression? other)
    {
        if (other is null)
        {
            return false;
        }

        if (Operands.Count != other.Operands.Count)
        {
            return false;
        }

        for (int i = 0; i < Operands.Count; i++)
        {
            if (!Operands[i].Equals(other.Operands[i]))
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>Reserved: logical NOT of a single operand. Not emitted in Sprint 10.</summary>
/// <param name="Operand">The negated expression.</param>
public sealed record NotExpression(SearchExpression Operand) : SearchExpression;

/// <summary>Reserved: explicit grouping for precedence. Not emitted in Sprint 10.</summary>
/// <param name="Inner">The grouped expression.</param>
public sealed record GroupExpression(SearchExpression Inner) : SearchExpression;
#pragma warning restore SA1402 // File may only contain a single type
