namespace Ferret.Cli.Cli;

/// <summary>Formats a command result model into CLI output. Inject a different implementation for --output json.</summary>
/// <typeparam name="T">The result model type.</typeparam>
internal interface ICommandResultFormatter<in T>
{
    /// <summary>Formats the result and writes it to the output formatter.</summary>
    /// <param name="result">The model to format.</param>
    /// <param name="output">The CLI output formatter to write to.</param>
    void Format(T result, IOutputFormatter output);
}
