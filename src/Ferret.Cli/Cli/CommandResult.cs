namespace Ferret.Cli.Cli;

/// <summary>Why: Typed command exit; maps to process exit codes. Thread Safety: Thread Safe — value type.</summary>
internal enum CommandResult
{
    /// <summary>Command completed successfully.</summary>
    Success = 0,

    /// <summary>Command failed.</summary>
    Failure = 1,

    /// <summary>Command was cancelled (SIGINT).</summary>
    Cancelled = 130,
}
