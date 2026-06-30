namespace Ferret.Core.Ai.Prompts;

/// <summary>An immutable, fluent container of named string bindings used to render a <see cref="PromptTemplate"/>.</summary>
public sealed class PromptVariables
{
    private readonly IReadOnlyDictionary<string, string> _values;

    private PromptVariables(IReadOnlyDictionary<string, string> values) => _values = values;

    /// <summary>Gets an empty <see cref="PromptVariables"/> instance with no bindings.</summary>
    public static PromptVariables Empty { get; } =
        new(new Dictionary<string, string>(StringComparer.Ordinal));

    /// <summary>Gets the names of all currently bound variables.</summary>
    public IReadOnlyList<string> Keys => [.. _values.Keys];

    /// <summary>Returns a new <see cref="PromptVariables"/> with <paramref name="name"/> bound to <paramref name="value"/>.</summary>
    /// <param name="name">The variable name (case-sensitive).</param>
    /// <param name="value">The string value to bind.</param>
    /// <returns>A new <see cref="PromptVariables"/> instance containing all existing bindings plus the new one.</returns>
    public PromptVariables Set(string name, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        var next = new Dictionary<string, string>(_values, StringComparer.Ordinal) { [name] = value };
        return new PromptVariables(next);
    }

    /// <summary>Returns the value bound to <paramref name="name"/>, or <see langword="null"/> if not set.</summary>
    /// <param name="name">The variable name to look up.</param>
    /// <returns>The bound string value, or <see langword="null"/>.</returns>
    public string? TryGet(string name) =>
        _values.TryGetValue(name, out var v) ? v : null;

    /// <summary>Returns the value bound to <paramref name="name"/>, or throws if not set.</summary>
    /// <param name="name">The variable name to look up.</param>
    /// <returns>The bound string value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="name"/> has no binding.</exception>
    public string GetRequired(string name) =>
        TryGet(name) ?? throw new InvalidOperationException(
            $"Required prompt variable '{name}' is not set.");

    /// <summary>Returns <see langword="true"/> if a binding exists for <paramref name="name"/>.</summary>
    /// <param name="name">The variable name to check.</param>
    /// <returns><see langword="true"/> when the key is present; <see langword="false"/> otherwise.</returns>
    public bool Contains(string name) => _values.ContainsKey(name);
}
