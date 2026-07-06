namespace Ferret.Cli.Commands.Handlers;

/// <summary>A snapshot of a running <c>ferret start</c> process, as recorded by <see cref="RuntimeStatusFile"/>.</summary>
internal sealed record RuntimeStatusRecord(int ProcessId, DateTimeOffset StartedAtUtc);
