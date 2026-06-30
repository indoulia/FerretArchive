namespace Ferret.Cli.Cli;

/// <summary>
/// Why: The only class referencing console output directly. Plain text only — no ANSI, no color.
/// Layer: Ferret.Cli only.
/// Thread Safety: Single Thread Only.
/// </summary>
internal sealed class ConsoleFormatter : IOutputFormatter
{
    private const string CheckMark = "✓";
    private const string CrossMark = "✗";

    private readonly TextWriter _out;
    private readonly bool _verbose;

    /// <summary>Initializes a new instance of the <see cref="ConsoleFormatter"/> class.</summary>
    /// <param name="out">The underlying text writer.</param>
    /// <param name="verbosity">The verbosity level. Defaults to Normal.</param>
    internal ConsoleFormatter(TextWriter @out, VerbosityLevel verbosity = VerbosityLevel.Normal)
    {
        _out = @out;
        _verbose = verbosity == VerbosityLevel.Verbose;
    }

    /// <inheritdoc/>
    public void WriteLine(string text = "") => _out.WriteLine(text);

    /// <inheritdoc/>
    public void WriteSuccess(string message) => _out.WriteLine($"{CheckMark} {message}");

    /// <inheritdoc/>
    public void WriteError(string message) => _out.WriteLine($"{CrossMark} {message}");

    /// <inheritdoc/>
    public void WriteVerbose(string message)
    {
        if (_verbose)
        {
            _out.WriteLine(message);
        }
    }
}
