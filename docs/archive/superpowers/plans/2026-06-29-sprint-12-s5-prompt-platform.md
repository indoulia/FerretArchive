# Sprint 12 Sub-plan 5 — Prompt Platform (`Ferret.Prompts`)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a prompt template management platform — `Ferret.Prompts` — that lets feature packages register `PromptTemplate` instances at startup and any consumer render them at runtime by supplying `PromptVariables`. Templates use `{{variable}}` placeholder syntax. The registry is immutable after construction; the renderer is stateless.

**Architecture:** `Ferret.Prompts` is a pure platform library with no external NuGet references. `PromptRegistry` is built from `IEnumerable<PromptTemplate>` registered in DI — feature packages supply templates; the registry collects them. `PromptRenderer` is stateless regex substitution. `PromptRenderException` is the only exception type, deriving from `FerretException`. `PromptsModule` wires all types as singletons.

**Tech Stack:** .NET 9, C# 13, xUnit, `System.Text.RegularExpressions` (BCL only — no external NuGet references)

## Global Constraints

- Sprint 11 must be fully implemented before Sprint 12. Assumes `FerretException` in `Ferret.Core.Errors`.
- All tasks: TDD — write failing test first, confirm red, implement, verify green.
- Commit prefix: `feat(sprint-12):`.
- Namespace: `Ferret.Prompts`.
- No external NuGet references in `Ferret.Prompts` or its test project.
- `PromptTemplate` is a `sealed record`; `PromptVariables` is a `sealed class` with fluent API (immutable — `Set` returns a new instance).
- `PromptRegistry` is immutable after construction — duplicate `Name+Version` registration throws at construction time.
- `PromptRenderer` is stateless — `{{var}}` not in variables and not in `RequiredVariables` is left as-is in output.
- `GetLatest` resolves highest version by semantic parsing (`System.Version.TryParse`; non-parseable versions sort last).
- Build command: `dotnet build src/Ferret.Prompts/Ferret.Prompts.csproj -v n`
- Test command: `dotnet test tests/Ferret.Prompts.Tests/ -v n`
- Full solution: `dotnet test src/Ferret.sln -v n`

---

## File Structure Map

```
src/Ferret.Prompts/
  Ferret.Prompts.csproj              [NEW — Task 4] references Ferret.Core only
  PromptTemplate.cs                  [NEW — Task 1]
  PromptVariables.cs                 [NEW — Task 1]
  IPromptRegistry.cs                 [NEW — Task 2]
  PromptRegistry.cs                  [NEW — Task 2]
  IPromptRenderer.cs                 [NEW — Task 3]
  PromptRenderer.cs                  [NEW — Task 3]
  Exceptions/
    PromptRenderException.cs         [NEW — Task 3]
  PromptsModule.cs                   [NEW — Task 4]

tests/Ferret.Prompts.Tests/
  Ferret.Prompts.Tests.csproj        [NEW — Task 4]
  PromptTemplateTests.cs             [NEW — Task 1]
  PromptVariablesTests.cs            [NEW — Task 1]
  PromptRegistryTests.cs             [NEW — Task 2]
  PromptRendererTests.cs             [NEW — Task 3]
```

---

### Task 1: PromptTemplate + PromptVariables — Core Types

Defines the two fundamental value types. `PromptTemplate` is an immutable record carrying a template string and metadata. `PromptVariables` is a fluent immutable container: each `Set` call returns a new instance with the added binding.

**Files:**
- Create: `src/Ferret.Prompts/PromptTemplate.cs`
- Create: `src/Ferret.Prompts/PromptVariables.cs`
- Create: `tests/Ferret.Prompts.Tests/PromptTemplateTests.cs`
- Create: `tests/Ferret.Prompts.Tests/PromptVariablesTests.cs`

**Interfaces:**
- Consumes: nothing (no dependencies yet — project files created in Task 4, but scaffold the source files so they can be added to the project then)
- Produces: `PromptTemplate (sealed record)`, `PromptVariables (sealed class)`

> **Note:** The test project (`Ferret.Prompts.Tests.csproj`) and the source project (`Ferret.Prompts.csproj`) are created in Task 4. Write the source files in Task 1 and run the build/test after Task 4 sets up the project files. Alternatively, set up the project files first (Task 4 step 1) and then do Tasks 1-3.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Prompts.Tests/PromptTemplateTests.cs
using Ferret.Prompts;
using Xunit;

namespace Ferret.Prompts.Tests;

public sealed class PromptTemplateTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var template = new PromptTemplate
        {
            Name = "workspace-context",
            Version = "1.0.0",
            Template = "Hello {{name}}",
            RequiredVariables = ["name"],
            Description = "A greeting template"
        };

        Assert.Equal("workspace-context", template.Name);
        Assert.Equal("1.0.0", template.Version);
        Assert.Equal("Hello {{name}}", template.Template);
        Assert.Single(template.RequiredVariables);
        Assert.Equal("name", template.RequiredVariables[0]);
        Assert.Equal("A greeting template", template.Description);
    }

    [Fact]
    public void Description_IsOptional_DefaultsToNull()
    {
        var template = new PromptTemplate
        {
            Name = "t",
            Version = "1.0.0",
            Template = "hello",
            RequiredVariables = []
        };

        Assert.Null(template.Description);
    }

    [Fact]
    public void RecordEquality_SameProperties_AreEqual()
    {
        var a = new PromptTemplate { Name = "t", Version = "1.0.0", Template = "x", RequiredVariables = [] };
        var b = new PromptTemplate { Name = "t", Version = "1.0.0", Template = "x", RequiredVariables = [] };

        Assert.Equal(a, b);
    }
}
```

```csharp
// tests/Ferret.Prompts.Tests/PromptVariablesTests.cs
using Ferret.Prompts;
using Xunit;

namespace Ferret.Prompts.Tests;

public sealed class PromptVariablesTests
{
    [Fact]
    public void Empty_HasNoKeys()
    {
        Assert.Empty(PromptVariables.Empty.Keys);
    }

    [Fact]
    public void Set_AddsBinding_ReturnsNewInstance()
    {
        var original = PromptVariables.Empty;
        var updated = original.Set("name", "Alice");

        // immutable — original unchanged
        Assert.Empty(original.Keys);
        Assert.Single(updated.Keys);
        Assert.Equal("Alice", updated.TryGet("name"));
    }

    [Fact]
    public void Set_ChainedCalls_AllBindingsPresent()
    {
        var vars = PromptVariables.Empty
            .Set("a", "1")
            .Set("b", "2")
            .Set("c", "3");

        Assert.Equal(3, vars.Keys.Count);
        Assert.Equal("1", vars.TryGet("a"));
        Assert.Equal("2", vars.TryGet("b"));
        Assert.Equal("3", vars.TryGet("c"));
    }

    [Fact]
    public void TryGet_MissingKey_ReturnsNull()
    {
        Assert.Null(PromptVariables.Empty.TryGet("missing"));
    }

    [Fact]
    public void GetRequired_PresentKey_ReturnsValue()
    {
        var vars = PromptVariables.Empty.Set("key", "value");
        Assert.Equal("value", vars.GetRequired("key"));
    }

    [Fact]
    public void GetRequired_MissingKey_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            PromptVariables.Empty.GetRequired("missing"));
    }

    [Fact]
    public void Contains_PresentKey_ReturnsTrue()
    {
        var vars = PromptVariables.Empty.Set("x", "1");
        Assert.True(vars.Contains("x"));
    }

    [Fact]
    public void Contains_AbsentKey_ReturnsFalse()
    {
        Assert.False(PromptVariables.Empty.Contains("x"));
    }

    [Fact]
    public void Set_OverwritesExistingKey()
    {
        var vars = PromptVariables.Empty.Set("key", "first").Set("key", "second");
        Assert.Equal("second", vars.TryGet("key"));
        Assert.Single(vars.Keys);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Prompts.Tests/ -v n
```

Expected: compile errors — types not found (project must be set up first per Task 4 step 1; if running in sequence set up csproj before this step).

- [ ] **Step 3: Write PromptTemplate**

```csharp
// src/Ferret.Prompts/PromptTemplate.cs
namespace Ferret.Prompts;

public sealed record PromptTemplate
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required string Template { get; init; }
    public required IReadOnlyList<string> RequiredVariables { get; init; }
    public string? Description { get; init; }
}
```

- [ ] **Step 4: Write PromptVariables**

```csharp
// src/Ferret.Prompts/PromptVariables.cs
namespace Ferret.Prompts;

public sealed class PromptVariables
{
    private readonly IReadOnlyDictionary<string, string> _values;

    private PromptVariables(IReadOnlyDictionary<string, string> values) => _values = values;

    public static PromptVariables Empty { get; } =
        new(new Dictionary<string, string>(StringComparer.Ordinal));

    public PromptVariables Set(string name, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        var next = new Dictionary<string, string>(_values, StringComparer.Ordinal)
        {
            [name] = value
        };
        return new PromptVariables(next);
    }

    public string? TryGet(string name) =>
        _values.TryGetValue(name, out var v) ? v : null;

    public string GetRequired(string name) =>
        TryGet(name) ?? throw new InvalidOperationException(
            $"Required prompt variable '{name}' is not set.");

    public bool Contains(string name) => _values.ContainsKey(name);

    public IReadOnlyList<string> Keys => [.. _values.Keys];
}
```

- [ ] **Step 5: Run tests to verify they pass**

```
dotnet test tests/Ferret.Prompts.Tests/ --filter "FullyQualifiedName~PromptTemplate|FullyQualifiedName~PromptVariables" -v n
```

Expected: 12 tests PASS.

- [ ] **Step 6: Commit**

```
git add src/Ferret.Prompts/PromptTemplate.cs src/Ferret.Prompts/PromptVariables.cs tests/Ferret.Prompts.Tests/PromptTemplateTests.cs tests/Ferret.Prompts.Tests/PromptVariablesTests.cs
git commit -m "feat(sprint-12): PromptTemplate record + PromptVariables fluent immutable container"
```

---

### Task 2: IPromptRegistry + PromptRegistry — Template Registry

The registry is built from `IEnumerable<PromptTemplate>` via DI. It is immutable after construction. Duplicate `Name+Version` combinations throw at construction time. `GetLatest` picks the highest semantic version; non-parseable version strings sort last (treated as lower than any parseable version).

**Files:**
- Create: `src/Ferret.Prompts/IPromptRegistry.cs`
- Create: `src/Ferret.Prompts/PromptRegistry.cs`
- Create: `tests/Ferret.Prompts.Tests/PromptRegistryTests.cs`

**Interfaces:**
- Consumes: `PromptTemplate` (Task 1)
- Produces: `IPromptRegistry`, `PromptRegistry (sealed class)`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Prompts.Tests/PromptRegistryTests.cs
using Ferret.Prompts;
using Xunit;

namespace Ferret.Prompts.Tests;

public sealed class PromptRegistryTests
{
    private static PromptTemplate Make(string name, string version, string template = "hello") =>
        new() { Name = name, Version = version, Template = template, RequiredVariables = [] };

    [Fact]
    public void Get_RegisteredTemplate_ReturnsTemplate()
    {
        var t = Make("greet", "1.0.0");
        var registry = new PromptRegistry([t]);

        var result = registry.Get("greet", "1.0.0");

        Assert.NotNull(result);
        Assert.Equal("greet", result.Name);
        Assert.Equal("1.0.0", result.Version);
    }

    [Fact]
    public void Get_UnregisteredTemplate_ReturnsNull()
    {
        var registry = new PromptRegistry([]);
        Assert.Null(registry.Get("missing", "1.0.0"));
    }

    [Fact]
    public void GetLatest_MultipleVersions_ReturnsHighestSemVer()
    {
        var registry = new PromptRegistry([
            Make("greet", "1.0.0"),
            Make("greet", "2.0.0"),
            Make("greet", "1.5.0")
        ]);

        var latest = registry.GetLatest("greet");

        Assert.NotNull(latest);
        Assert.Equal("2.0.0", latest.Version);
    }

    [Fact]
    public void GetLatest_SingleVersion_ReturnsThatVersion()
    {
        var registry = new PromptRegistry([Make("greet", "1.0.0")]);
        var latest = registry.GetLatest("greet");

        Assert.NotNull(latest);
        Assert.Equal("1.0.0", latest.Version);
    }

    [Fact]
    public void GetLatest_NoTemplatesWithName_ReturnsNull()
    {
        var registry = new PromptRegistry([Make("other", "1.0.0")]);
        Assert.Null(registry.GetLatest("missing"));
    }

    [Fact]
    public void GetAll_ReturnsAllTemplates()
    {
        var templates = new[]
        {
            Make("a", "1.0.0"),
            Make("b", "1.0.0"),
            Make("a", "2.0.0")
        };
        var registry = new PromptRegistry(templates);

        Assert.Equal(3, registry.GetAll().Count);
    }

    [Fact]
    public void Register_DuplicateNameVersion_Throws()
    {
        var t = Make("greet", "1.0.0");
        Assert.Throws<InvalidOperationException>(() => new PromptRegistry([t, t]));
    }

    [Fact]
    public void GetLatest_NonParsableVersion_SortsLast()
    {
        // "beta" can't be parsed as System.Version — parseable 1.0.0 wins
        var registry = new PromptRegistry([
            Make("greet", "beta"),
            Make("greet", "1.0.0")
        ]);

        var latest = registry.GetLatest("greet");
        Assert.Equal("1.0.0", latest!.Version);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Prompts.Tests/ --filter "FullyQualifiedName~PromptRegistry" -v n
```

Expected: compile errors — `IPromptRegistry`, `PromptRegistry` not found.

- [ ] **Step 3: Write IPromptRegistry**

```csharp
// src/Ferret.Prompts/IPromptRegistry.cs
namespace Ferret.Prompts;

public interface IPromptRegistry
{
    /// <summary>Returns the template with the given name and version, or null if not found.</summary>
    PromptTemplate? Get(string name, string version);

    /// <summary>Returns the highest-version template with the given name, or null if none registered.</summary>
    PromptTemplate? GetLatest(string name);

    /// <summary>Returns all registered templates.</summary>
    IReadOnlyList<PromptTemplate> GetAll();
}
```

- [ ] **Step 4: Write PromptRegistry**

```csharp
// src/Ferret.Prompts/PromptRegistry.cs
namespace Ferret.Prompts;

public sealed class PromptRegistry : IPromptRegistry
{
    private readonly IReadOnlyList<PromptTemplate> _all;
    private readonly IReadOnlyDictionary<string, PromptTemplate> _byKey;

    public PromptRegistry(IEnumerable<PromptTemplate> templates)
    {
        ArgumentNullException.ThrowIfNull(templates);

        var list = templates.ToList();
        var byKey = new Dictionary<string, PromptTemplate>(StringComparer.Ordinal);

        foreach (var template in list)
        {
            var key = MakeKey(template.Name, template.Version);
            if (!byKey.TryAdd(key, template))
                throw new InvalidOperationException(
                    $"Prompt template '{template.Name}' version '{template.Version}' is already registered.");
        }

        _all = list;
        _byKey = byKey;
    }

    public PromptTemplate? Get(string name, string version) =>
        _byKey.TryGetValue(MakeKey(name, version), out var t) ? t : null;

    public PromptTemplate? GetLatest(string name)
    {
        var candidates = _all
            .Where(t => string.Equals(t.Name, name, StringComparison.Ordinal))
            .ToList();

        if (candidates.Count == 0)
            return null;

        return candidates
            .OrderByDescending(t => t.Version, VersionComparer.Instance)
            .First();
    }

    public IReadOnlyList<PromptTemplate> GetAll() => _all;

    private static string MakeKey(string name, string version) => $"{name}@{version}";

    // Sorts parseable semantic versions descending; non-parseable versions sort last.
    private sealed class VersionComparer : IComparer<string>
    {
        public static readonly VersionComparer Instance = new();

        public int Compare(string? x, string? y)
        {
            var xParsed = System.Version.TryParse(x, out var xv);
            var yParsed = System.Version.TryParse(y, out var yv);

            return (xParsed, yParsed) switch
            {
                (true, true) => xv!.CompareTo(yv),
                (true, false) => 1,   // parseable > non-parseable
                (false, true) => -1,
                _ => StringComparer.Ordinal.Compare(x, y)
            };
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```
dotnet test tests/Ferret.Prompts.Tests/ --filter "FullyQualifiedName~PromptRegistry" -v n
```

Expected: 8 tests PASS.

- [ ] **Step 6: Commit**

```
git add src/Ferret.Prompts/IPromptRegistry.cs src/Ferret.Prompts/PromptRegistry.cs tests/Ferret.Prompts.Tests/PromptRegistryTests.cs
git commit -m "feat(sprint-12): IPromptRegistry + PromptRegistry — immutable template registry with semantic version ordering"
```

---

### Task 3: IPromptRenderer + PromptRenderer + PromptRenderException — Rendering

`PromptRenderer` is a stateless class. It substitutes all `{{variable}}` placeholders in a template string. If any `RequiredVariable` is missing from the supplied `PromptVariables`, it throws `PromptRenderException`. Optional variables (`{{var}}` in the template but not in `RequiredVariables`) are left as-is in output if no binding is provided. `Validate` returns the list of missing required variable names without throwing.

**Files:**
- Create: `src/Ferret.Prompts/IPromptRenderer.cs`
- Create: `src/Ferret.Prompts/PromptRenderer.cs`
- Create: `src/Ferret.Prompts/Exceptions/PromptRenderException.cs`
- Create: `tests/Ferret.Prompts.Tests/PromptRendererTests.cs`

**Interfaces:**
- Consumes: `PromptTemplate`, `PromptVariables` (Task 1), `FerretException` from `Ferret.Core.Errors`
- Produces: `IPromptRenderer`, `PromptRenderer (sealed class)`, `PromptRenderException (sealed class : FerretException)`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Prompts.Tests/PromptRendererTests.cs
using Ferret.Prompts;
using Ferret.Prompts.Exceptions;
using Xunit;

namespace Ferret.Prompts.Tests;

public sealed class PromptRendererTests
{
    private readonly IPromptRenderer _sut = new PromptRenderer();

    private static PromptTemplate Make(string template, params string[] required) =>
        new()
        {
            Name = "test",
            Version = "1.0.0",
            Template = template,
            RequiredVariables = required
        };

    [Fact]
    public void Render_AllRequiredVariablesPresent_ReturnsRenderedString()
    {
        var t = Make("Hello {{name}}, you are {{age}} years old.", "name", "age");
        var vars = PromptVariables.Empty.Set("name", "Alice").Set("age", "30");

        var result = _sut.Render(t, vars);

        Assert.Equal("Hello Alice, you are 30 years old.", result);
    }

    [Fact]
    public void Render_MissingRequiredVariable_ThrowsPromptRenderException()
    {
        var t = Make("Hello {{name}}", "name");
        var vars = PromptVariables.Empty; // name not set

        var ex = Assert.Throws<PromptRenderException>(() => _sut.Render(t, vars));
        Assert.Equal("test", ex.TemplateName);
        Assert.Single(ex.MissingVariables);
        Assert.Equal("name", ex.MissingVariables[0]);
    }

    [Fact]
    public void Render_ExtraVariables_NotInTemplate_RendersSuccessfully()
    {
        var t = Make("Hello {{name}}", "name");
        var vars = PromptVariables.Empty.Set("name", "Bob").Set("unused", "value");

        var result = _sut.Render(t, vars);

        Assert.Equal("Hello Bob", result);
    }

    [Fact]
    public void Render_NoRequiredVariables_RendersSuccessfully()
    {
        var t = Make("Static text with no placeholders");
        var result = _sut.Render(t, PromptVariables.Empty);

        Assert.Equal("Static text with no placeholders", result);
    }

    [Fact]
    public void Render_OptionalPlaceholder_NoBinding_LeftAsIs()
    {
        // {{optional}} is in the template but NOT in RequiredVariables and NOT in vars
        var t = Make("Value: {{optional}}", /* no required variables */);
        var result = _sut.Render(t, PromptVariables.Empty);

        Assert.Equal("Value: {{optional}}", result);
    }

    [Fact]
    public void Render_OptionalPlaceholder_BindingProvided_Substituted()
    {
        var t = Make("Value: {{optional}}", /* no required variables */);
        var vars = PromptVariables.Empty.Set("optional", "42");

        var result = _sut.Render(t, vars);

        Assert.Equal("Value: 42", result);
    }

    [Fact]
    public void Render_MultipleMissingRequired_ExceptionListsAll()
    {
        var t = Make("{{a}} and {{b}} and {{c}}", "a", "b", "c");
        var vars = PromptVariables.Empty; // all missing

        var ex = Assert.Throws<PromptRenderException>(() => _sut.Render(t, vars));
        Assert.Equal(3, ex.MissingVariables.Count);
        Assert.Contains("a", ex.MissingVariables);
        Assert.Contains("b", ex.MissingVariables);
        Assert.Contains("c", ex.MissingVariables);
    }

    [Fact]
    public void Validate_AllPresent_ReturnsEmptyList()
    {
        var t = Make("{{name}}", "name");
        var vars = PromptVariables.Empty.Set("name", "Alice");

        var missing = _sut.Validate(t, vars);

        Assert.Empty(missing);
    }

    [Fact]
    public void Validate_SomeMissing_ReturnsMissingNames()
    {
        var t = Make("{{a}} {{b}}", "a", "b");
        var vars = PromptVariables.Empty.Set("a", "1"); // b missing

        var missing = _sut.Validate(t, vars);

        Assert.Single(missing);
        Assert.Equal("b", missing[0]);
    }

    [Fact]
    public void PromptRenderException_MessageContainsTemplateNameAndVariables()
    {
        var t = Make("{{x}}", "x");
        var ex = Assert.Throws<PromptRenderException>(() =>
            _sut.Render(t, PromptVariables.Empty));

        Assert.Contains("test", ex.Message);
        Assert.Contains("x", ex.Message);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Prompts.Tests/ --filter "FullyQualifiedName~PromptRenderer" -v n
```

Expected: compile errors — `IPromptRenderer`, `PromptRenderer`, `PromptRenderException` not found.

- [ ] **Step 3: Write PromptRenderException**

```csharp
// src/Ferret.Prompts/Exceptions/PromptRenderException.cs
using Ferret.Core.Errors;

namespace Ferret.Prompts.Exceptions;

public sealed class PromptRenderException : FerretException
{
    public string TemplateName { get; }
    public IReadOnlyList<string> MissingVariables { get; }

    public PromptRenderException(string templateName, IReadOnlyList<string> missingVariables)
        : base(BuildMessage(templateName, missingVariables))
    {
        TemplateName = templateName;
        MissingVariables = missingVariables;
    }

    private static string BuildMessage(string name, IReadOnlyList<string> missing)
    {
        var list = string.Join(", ", missing.Select(v => $"'{v}'"));
        return $"Prompt '{name}' requires variable(s) {list} which were not provided.";
    }
}
```

- [ ] **Step 4: Write IPromptRenderer**

```csharp
// src/Ferret.Prompts/IPromptRenderer.cs
namespace Ferret.Prompts;

public interface IPromptRenderer
{
    /// <summary>
    /// Renders the template by substituting {{variable}} placeholders from the supplied variables.
    /// Throws <see cref="Exceptions.PromptRenderException"/> if any RequiredVariable is absent.
    /// Optional placeholders with no binding are left as-is in the output.
    /// </summary>
    string Render(PromptTemplate template, PromptVariables variables);

    /// <summary>
    /// Returns the list of required variable names that are absent from the supplied variables.
    /// Returns an empty list when all required variables are present.
    /// </summary>
    IReadOnlyList<string> Validate(PromptTemplate template, PromptVariables variables);
}
```

- [ ] **Step 5: Write PromptRenderer**

```csharp
// src/Ferret.Prompts/PromptRenderer.cs
using System.Text.RegularExpressions;
using Ferret.Prompts.Exceptions;

namespace Ferret.Prompts;

public sealed class PromptRenderer : IPromptRenderer
{
    // Matches {{variable_name}} — variable names may contain letters, digits, underscores, hyphens
    private static readonly Regex s_placeholder =
        new(@"\{\{([a-zA-Z0-9_\-]+)\}\}", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    public string Render(PromptTemplate template, PromptVariables variables)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(variables);

        var missing = Validate(template, variables);
        if (missing.Count > 0)
            throw new PromptRenderException(template.Name, missing);

        return s_placeholder.Replace(template.Template, match =>
        {
            var name = match.Groups[1].Value;
            return variables.TryGet(name) ?? match.Value; // leave as-is if no binding
        });
    }

    public IReadOnlyList<string> Validate(PromptTemplate template, PromptVariables variables)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(variables);

        return template.RequiredVariables
            .Where(v => !variables.Contains(v))
            .ToList();
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```
dotnet test tests/Ferret.Prompts.Tests/ --filter "FullyQualifiedName~PromptRenderer" -v n
```

Expected: 10 tests PASS.

- [ ] **Step 7: Commit**

```
git add src/Ferret.Prompts/IPromptRenderer.cs src/Ferret.Prompts/PromptRenderer.cs src/Ferret.Prompts/Exceptions/PromptRenderException.cs tests/Ferret.Prompts.Tests/PromptRendererTests.cs
git commit -m "feat(sprint-12): IPromptRenderer + PromptRenderer + PromptRenderException — regex-based template rendering"
```

---

### Task 4: Project Setup + PromptsModule + Full Solution Test

Creates the two `.csproj` files, writes `PromptsModule` (the DI composition root), adds `Ferret.Prompts` to the solution file, and verifies the full solution builds and all tests pass.

**Files:**
- Create: `src/Ferret.Prompts/Ferret.Prompts.csproj`
- Create: `tests/Ferret.Prompts.Tests/Ferret.Prompts.Tests.csproj`
- Create: `src/Ferret.Prompts/PromptsModule.cs`
- Modify: `src/Ferret.sln` — add both new projects

**Interfaces:**
- Consumes: `IPromptRegistry`, `PromptRegistry`, `IPromptRenderer`, `PromptRenderer`, `IServiceCollection` from `Microsoft.Extensions.DependencyInjection`
- Produces: `PromptsModule.ConfigureServices(IServiceCollection services)`

> **Note:** This task is ideally run first (before Tasks 1-3) to create the project scaffolding so the source files can compile as they are written. If done in order (Tasks 1-3 first), the source files already exist — just create the csproj files and add them to the solution to make everything compile.

- [ ] **Step 1: Create Ferret.Prompts.csproj**

```xml
<!-- src/Ferret.Prompts/Ferret.Prompts.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Prompts</AssemblyName>
    <RootNamespace>Ferret.Prompts</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="Ferret.Prompts.Tests" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create Ferret.Prompts.Tests.csproj**

```xml
<!-- tests/Ferret.Prompts.Tests/Ferret.Prompts.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Ferret.Prompts.Tests</AssemblyName>
    <RootNamespace>Ferret.Prompts.Tests</RootNamespace>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Prompts\Ferret.Prompts.csproj" />
  </ItemGroup>

</Project>
```

> **Note:** Package versions are managed centrally by `Directory.Build.props` / `Directory.Packages.props` in the repo root. Do not specify version attributes here — follow the pattern in other test projects (e.g., `tests/Ferret.Mcp.Tests/Ferret.Mcp.Tests.csproj`).

- [ ] **Step 3: Add projects to the solution**

```
dotnet sln src/Ferret.sln add src/Ferret.Prompts/Ferret.Prompts.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Prompts.Tests/Ferret.Prompts.Tests.csproj
```

- [ ] **Step 4: Write PromptsModule**

```csharp
// src/Ferret.Prompts/PromptsModule.cs
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Prompts;

public static class PromptsModule
{
    /// <summary>
    /// Registers IPromptRegistry (singleton) and IPromptRenderer (singleton).
    /// Feature packages register their PromptTemplate instances before calling this method.
    /// PromptRegistry is built from all IEnumerable&lt;PromptTemplate&gt; registered in the container.
    /// </summary>
    public static void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IPromptRegistry>(sp =>
        {
            var templates = sp.GetService<IEnumerable<PromptTemplate>>() ?? [];
            return new PromptRegistry(templates);
        });

        services.AddSingleton<IPromptRenderer, PromptRenderer>();
    }
}
```

- [ ] **Step 5: Build the Ferret.Prompts project**

```
dotnet build src/Ferret.Prompts/Ferret.Prompts.csproj -v n
```

Expected: build succeeds with no errors.

- [ ] **Step 6: Run Ferret.Prompts.Tests**

```
dotnet test tests/Ferret.Prompts.Tests/ -v n
```

Expected: all tests PASS (3 PromptTemplateTests + 9 PromptVariablesTests + 8 PromptRegistryTests + 10 PromptRendererTests = 30 tests).

- [ ] **Step 7: Run full solution**

```
dotnet test src/Ferret.sln -v n
```

Expected: all tests PASS. No regressions in existing test projects.

- [ ] **Step 8: Commit**

```
git add src/Ferret.Prompts/Ferret.Prompts.csproj src/Ferret.Prompts/PromptsModule.cs tests/Ferret.Prompts.Tests/Ferret.Prompts.Tests.csproj src/Ferret.sln
git commit -m "feat(sprint-12): Ferret.Prompts project setup + PromptsModule DI wiring — prompt platform complete"
```
