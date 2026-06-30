namespace Ferret.Cli.Search;

/// <summary>
/// No-op implementation of <see cref="ITextStyler"/>. All methods return text unchanged.
/// Used when the user passes <c>--no-highlight</c> or when output is piped to a non-TTY.
/// </summary>
public sealed class NullTextStyler : ITextStyler
{
    /// <inheritdoc/>
    public string Match(string text) => text;

    /// <inheritdoc/>
    public string Muted(string text) => text;

    /// <inheritdoc/>
    public string Normal(string text) => text;
}
