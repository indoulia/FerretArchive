namespace Ferret.Core.Documents;

/// <summary>Describes the outcome of a parse dispatch attempt.</summary>
public enum ParseResultKind
{
    /// <summary>The content was parsed successfully and a Document was produced.</summary>
    Success = 0,

    /// <summary>No parser is registered for the asset's media type.</summary>
    Unsupported = 1,

    /// <summary>The content stream was empty or contained only whitespace.</summary>
    Empty = 2,

    /// <summary>The parser encountered an error during parsing. See Diagnostics for detail.</summary>
    Failed = 3,
}
