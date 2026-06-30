using Ferret.Core.Ai.Prompts;

namespace Ferret.Prompts;

/// <summary>Immutable in-memory catalogue of <see cref="PromptTemplate"/> instances.</summary>
public sealed class PromptRegistry : IPromptRegistry
{
    private readonly IReadOnlyList<PromptTemplate> _all;
    private readonly Dictionary<string, PromptTemplate> _byKey;

    /// <summary>Initializes a new instance of the <see cref="PromptRegistry"/> class.</summary>
    /// <param name="templates">The templates to register; must not contain duplicate Name+Version pairs.</param>
    /// <exception cref="InvalidOperationException">Thrown when two templates share the same Name and Version.</exception>
    public PromptRegistry(IEnumerable<PromptTemplate> templates)
    {
        var list = templates.ToList();
        var index = new Dictionary<string, PromptTemplate>(StringComparer.Ordinal);
        foreach (var t in list)
        {
            var key = $"{t.Name}@{t.Version}";
            if (!index.TryAdd(key, t))
            {
                throw new InvalidOperationException(
                    $"Duplicate prompt template: name='{t.Name}' version='{t.Version}'.");
            }
        }

        _all = list.AsReadOnly();
        _byKey = index;
    }

    /// <inheritdoc/>
    public PromptTemplate? GetByVersion(string name, string version) =>
        _byKey.TryGetValue($"{name}@{version}", out var t) ? t : null;

    /// <inheritdoc/>
    public PromptTemplate? GetLatest(string name) =>
        _all
            .Where(t => string.Equals(t.Name, name, StringComparison.Ordinal))
            .OrderByDescending(t => t.Version, new VersionComparer())
            .FirstOrDefault();

    /// <inheritdoc/>
    public IReadOnlyList<PromptTemplate> GetAll() => _all;

    private sealed class VersionComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            var xv = Version.TryParse(x, out var xp);
            var yv = Version.TryParse(y, out var yp);
            return (xv, yv) switch
            {
                (true, true) => xp!.CompareTo(yp),
                (true, false) => 1,
                (false, true) => -1,
                _ => StringComparer.Ordinal.Compare(x, y),
            };
        }
    }
}
