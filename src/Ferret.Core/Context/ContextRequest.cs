namespace Ferret.Core.Context;

/// <summary>Input to the context assembly pipeline.</summary>
public sealed record ContextRequest
{
    private string _query = string.Empty;

    /// <summary>Gets the query to search for and assemble context around.</summary>
    public required string Query
    {
        get => _query;
        init
        {
            if (value == null)
            {
                throw new InvalidOperationException("Query cannot be null.");
            }

            _query = value;
        }
    }

    /// <summary>Gets the maximum token budget for the assembled context. Approximated at 4 chars per token.</summary>
    public int MaxTokens { get; init; } = 8000;

    /// <summary>Gets the maximum number of documents to include in the context package.</summary>
    public int MaxDocuments { get; init; } = 10;

    /// <summary>Gets a value indicating whether to prefer section-level content over full document text for large documents.</summary>
    public bool IncludeSections { get; init; } = true;
}
