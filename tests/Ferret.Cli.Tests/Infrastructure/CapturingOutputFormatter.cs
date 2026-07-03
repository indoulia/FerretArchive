using Ferret.Cli.Cli;

namespace Ferret.Cli.Tests.Infrastructure;

/// <summary>Captures formatter output for assertions. Records the raw text of every write.</summary>
internal sealed class CapturingOutputFormatter : IOutputFormatter
{
    private readonly List<string> _lines = [];

    public IReadOnlyList<string> Lines => _lines;

    public string Text => string.Join("\n", _lines);

    public void WriteLine(string text = "") => _lines.Add(text);

    public void WriteSuccess(string message) => _lines.Add("✓ " + message);

    public void WriteError(string message) => _lines.Add("✗ " + message);

    public void WriteVerbose(string message) => _lines.Add(message);
}
