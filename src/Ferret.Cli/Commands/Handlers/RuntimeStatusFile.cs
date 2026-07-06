using System.Diagnostics;
using System.Text.Json;

namespace Ferret.Cli.Commands.Handlers;

/// <summary>
/// Records/reads a small on-disk marker so a separate <c>ferret status</c> invocation (a brand-new
/// process; runtime state held in memory by <c>ferret start</c> is invisible to it) can report whether
/// a previously started runtime host is still actually alive.
/// </summary>
internal static class RuntimeStatusFile
{
    private const string FileName = "runtime-status.json";

    /// <summary>Resolves the status file path for the given working directory.</summary>
    internal static string ResolvePath(string workingDirectory) =>
        Path.Join(workingDirectory, Ferret.Core.Workspace.WorkspaceLayout.RootDirectoryName, FileName);

    /// <summary>Writes the current process's ID and start time to <paramref name="path"/>.</summary>
    internal static void Write(string path, int processId, DateTimeOffset startedAtUtc)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(new RuntimeStatusRecord(processId, startedAtUtc));
        File.WriteAllText(path, json);
    }

    /// <summary>Reads the status record at <paramref name="path"/>, or null if absent or unreadable.</summary>
    internal static RuntimeStatusRecord? TryRead(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<RuntimeStatusRecord>(File.ReadAllText(path));
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    /// <summary>Best-effort deletion of the status file at <paramref name="path"/>; never throws.</summary>
    internal static void Delete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // best-effort cleanup
        }
        catch (UnauthorizedAccessException)
        {
            // best-effort cleanup
        }
    }

    /// <summary>
    /// Returns whether a process with <paramref name="processId"/> currently exists and has not exited.
    /// Cannot distinguish the original process from a different one that reused the same PID after
    /// the original exited -- an accepted limitation of PID-based liveness checks.
    /// </summary>
    internal static bool IsProcessAlive(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
