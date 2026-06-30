# Pluggable Option Adapters for RootCommandFactory

**Date:** 2026-06-29
**Status:** Approved
**Scope:** `src/Ferret.Cli/Commands/RootCommandFactory.cs` — one file, no public API changes

---

## Problem

`RootCommandFactory` currently contains two explicit type-switch chains that must stay
in sync whenever a new CLR option type is added:

- `MakeOption` — selects `Option<bool>`, `Option<int>`, or `Option<string>` via nested ternaries
- `ParseOptions` — reads values via matching `is Option<bool>` / `is Option<int>` / cast-to-string guards

Adding a type (e.g., `double`, `Uri`, `enum`, `TimeSpan`) requires editing both methods.
Missing either causes a silent default-value bug at runtime.

---

## Goal

Replace both chains with an internal adapter registry: each adapter owns both option
creation and value extraction, so adding a new CLR type means one new class and one
dictionary entry — nothing else.

---

## Non-Goals

- The registry is **not** an extensibility surface. `ICliModule` implementations declare
  `OptionDefinition` only; they do not participate in parsing or register adapters.
- No new public API. All types introduced are `private` inside `RootCommandFactory`.
- No behavior changes. This is a pure internal refactor.

---

## Design

### Components

| Type | Kind | Role |
|---|---|---|
| `IOptionAdapter` | `private interface` | Contract: `CreateOption(def)` + `ExtractValue(opt, pr)` |
| `OptionAdapter<T>` | `private abstract class` | Base: implements `ExtractValue` once; subclasses override `CreateOption` |
| `BoolAdapter` | `private sealed class` | `Option<bool>`, no default |
| `IntAdapter` | `private sealed class` | `Option<int>`, wires `DefaultValueFactory` |
| `StringAdapter` | `private sealed class` | `Option<string>`, no default |
| `Adapters` | `private static readonly Dictionary<Type, IOptionAdapter>` | Registry keyed by CLR type |

All types are nested inside `RootCommandFactory`.

### IOptionAdapter

```csharp
private interface IOptionAdapter
{
    Option CreateOption(OptionDefinition def);
    object? ExtractValue(Option opt, ParseResult parseResult);
}
```

### OptionAdapter\<T\>

The abstract generic base bridges the non-generic interface to the typed API.
`ExtractValue` is implemented exactly once here; concrete subclasses override only
`CreateOption`:

```csharp
private abstract class OptionAdapter<T> : IOptionAdapter
{
    // Abstract: each adapter supplies its own Option<T> construction
    public abstract Option<T> CreateOption(OptionDefinition def);

    // Bridge: satisfies IOptionAdapter.CreateOption without losing generic CreateOption
    Option IOptionAdapter.CreateOption(OptionDefinition def) => CreateOption(def);

    // Final: generic cast is correct because the registry guarantees T matches ValueType
    public object? ExtractValue(Option opt, ParseResult parseResult) =>
        parseResult.GetValue((Option<T>)opt);
}
```

### Concrete adapters

```csharp
private sealed class BoolAdapter : OptionAdapter<bool>
{
    public override Option<bool> CreateOption(OptionDefinition def) =>
        new(def.LongName) { Description = def.Description };
}

private sealed class IntAdapter : OptionAdapter<int>
{
    public override Option<int> CreateOption(OptionDefinition def) =>
        new(def.LongName)
        {
            Description = def.Description,
            DefaultValueFactory = _ => def.DefaultValue is int d ? d : 0,
        };
}

private sealed class StringAdapter : OptionAdapter<string>
{
    public override Option<string> CreateOption(OptionDefinition def) =>
        new(def.LongName) { Description = def.Description };
}
```

### Registry

Adapters are immutable and stateless, so each is a named singleton field before the
dictionary. This makes the strategy-object pattern explicit and avoids anonymous
allocations inside the dictionary initializer.

```csharp
private static readonly BoolAdapter   BoolAdapterInstance   = new();
private static readonly IntAdapter    IntAdapterInstance    = new();
private static readonly StringAdapter StringAdapterInstance = new();

// Maps CLR primitive option types to the internal adapter responsible for
// creating and parsing System.CommandLine Option<T> instances.
// This registry is intentionally private; CLI modules declare only
// OptionDefinitions and never participate in parsing behavior.
//
// Scope: primitive CLR types (bool, int, string, …) only.
// Domain types (ModelId, WorkspaceId, SearchOptions, etc.) must NOT be
// registered here — parse domain values inside command handlers, not in
// the CLI framework.
private static readonly Dictionary<Type, IOptionAdapter> Adapters = new()
{
    [typeof(bool)]   = BoolAdapterInstance,
    [typeof(int)]    = IntAdapterInstance,
    [typeof(string)] = StringAdapterInstance,
};
```

### Simplified MakeOption

```csharp
private static Option MakeOption(OptionDefinition def)
{
    if (!Adapters.TryGetValue(def.ValueType, out var adapter))
        throw new NotSupportedException(
            $"No option adapter is registered for CLR type '{def.ValueType.Name}'.\n" +
            $"Either:\n" +
            $" - register an OptionAdapter<{def.ValueType.Name}> in RootCommandFactory.Adapters, or\n" +
            $" - change the option to use a supported CLR type.");

    var opt = adapter.CreateOption(def);
    opt.Hidden = def.IsHidden;
    return opt;
}
```

### Simplified ParseOptions

```csharp
private static Dictionary<string, object?> ParseOptions(
    ParseResult parseResult,
    Dictionary<string, Option> optMap)
{
    var result = new Dictionary<string, object?>(StringComparer.Ordinal);
    foreach (var (name, opt) in optMap)
        result[name] = Adapters[opt.ValueType].ExtractValue(opt, parseResult);
    return result;
}
```

`opt.ValueType` is documented on `System.CommandLine.Option` (verified in 2.0.9 XML docs):
"Gets the `Type` that the option's parsed tokens will be converted to."

No guard is needed in `ParseOptions`: if `MakeOption` succeeded, the type is in the
registry by construction.

---

## Data Flow

```
OptionDefinition.ValueType
        │
        ▼
Adapters.TryGetValue()
        │
        ├── Not found → NotSupportedException at startup (programming error)
        │
        └── Found → adapter.CreateOption(def) → Option<T>
                            │
                            ▼ (at invocation time)
                    opt.ValueType → Adapters[opt.ValueType]
                            │
                            └── adapter.ExtractValue(opt, pr) → object?
```

---

## Error Handling

One failure mode: an `OptionDefinition` declares a `ValueType` with no registered
adapter. This is a programming error, detected at startup (command construction), not
at runtime. The exception message names the type and offers both corrective actions.

`ParseOptions` carries no guard because the invariant holds: any `Option<T>` that
reaches `ParseOptions` was created by an adapter, so its `ValueType` is guaranteed
to be in the registry.

---

## Testing

This is a pure internal refactor. Existing tests that exercise typed options (e.g.,
`--port 7070` as `int`, `--verbose` as `bool`, string options) remain the primary
verification.

One new test should be added:

**`UnknownOptionType_ThrowsNotSupportedException`** — builds a command with an
`OptionDefinition` whose `ValueType` is not in the registry and asserts that
`NotSupportedException` is thrown. This documents and locks in the startup-fail-fast
guarantee.

Tests for individual private adapter methods (`BoolAdapter.CreateOption`, etc.) are
explicitly out of scope — they are private implementation details; testing through
observable behavior is sufficient.

---

## File Impact

| File | Change |
|---|---|
| `src/Ferret.Cli/Commands/RootCommandFactory.cs` | Add `IOptionAdapter`, `OptionAdapter<T>`, three concrete adapters, `Adapters` dict; simplify `MakeOption` and `ParseOptions` |
| `tests/Ferret.Cli.Tests/...` | Add one test for unsupported type |

No other files are touched. No public contracts change.

---

## Adding a Future Option Type

To support `double`:

1. Add `private sealed class DoubleAdapter : OptionAdapter<double>` with one `CreateOption` override.
2. Add `[typeof(double)] = new DoubleAdapter()` to `Adapters`.

Nothing else changes.
