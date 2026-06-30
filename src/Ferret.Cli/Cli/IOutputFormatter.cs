namespace Ferret.Cli.Cli;

/// <summary>
/// Why: Abstracts output medium; Sprint 7 adds JsonFormatter without touching commands.
/// Thread Safety: Single Thread Only.
/// </summary>
internal interface IOutputFormatter
{
    /// <summary>Writes a line of text, defaulting to empty.</summary>
    /// <param name="text">The text to write.</param>
    void WriteLine(string text = "");

    /// <summary>Writes a success message prefixed with ✓.</summary>
    /// <param name="message">The message to write.</param>
    void WriteSuccess(string message);

    /// <summary>Writes an error message prefixed with ✗.</summary>
    /// <param name="message">The message to write.</param>
    void WriteError(string message);

    /// <summary>Writes a verbose message; no-op unless verbosity is Verbose.</summary>
    /// <param name="message">The message to write.</param>
    void WriteVerbose(string message);
}
