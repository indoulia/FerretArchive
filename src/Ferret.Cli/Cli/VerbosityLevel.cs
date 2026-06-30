namespace Ferret.Cli.Cli;

/// <summary>Controls how much output is written to the console.</summary>
internal enum VerbosityLevel
{
    /// <summary>Suppress most output.</summary>
    Quiet,

    /// <summary>Standard output level.</summary>
    Normal,

    /// <summary>Emit detailed diagnostic output.</summary>
    Verbose,
}
