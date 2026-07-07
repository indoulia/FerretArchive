# Pluggable Option Adapters Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the two explicit type-switch chains in `RootCommandFactory` (`MakeOption` and `ParseOptions`) with an internal adapter registry where each adapter owns both option creation and value extraction.

**Architecture:** A private `IOptionAdapter` interface + abstract `OptionAdapter<T>` generic base lives entirely inside `RootCommandFactory`. Three concrete sealed adapters (`BoolAdapter`, `IntAdapter`, `StringAdapter`) override only `CreateOption`; the base class supplies `ExtractValue` once via a generic cast. A static `Dictionary<Type, IOptionAdapter>` keyed by CLR type replaces both if-chains with a single lookup. Everything is `private`; no public API changes.

**Tech Stack:** C# 13 / .NET 9, System.CommandLine 2.0.9, xUnit

## Global Constraints

- Target framework: net9.0
- No new public types, interfaces, or members — all additions are `private` nested inside `RootCommandFactory`
- No other files touched beyond the two listed below
- `ICliModule` implementations must not be changed — they stay declarative (`OptionDefinition` only)
- `opt.ValueType` is used in `ParseOptions` (documented on `System.CommandLine.Option` in 2.0.9 XML docs as "Gets the Type that the option's parsed tokens will be converted to")
- Registry scope: primitive CLR types (`bool`, `int`, `string`) only — no Ferret domain types

---

### Task 1: Adapter registry + failing test → green

**Files:**
- Modify: `src/Ferret.Cli/Commands/RootCommandFactory.cs`
- Create: `tests/Ferret.Cli.Tests/Commands/RootCommandFactoryAdapterTests.cs`

**Interfaces:**
- Produces: nothing new externally — `RootCommandFactory.Build` signature is unchanged
- Internal new types (all `private`): `IOptionAdapter`, `OptionAdapter<T>`, `BoolAdapter`, `IntAdapter`, `StringAdapter`, `Adapters`

---

- [ ] **Step 1: Write the failing test**

Create `tests/Ferret.Cli.Tests/Commands/RootCommandFactoryAdapterTests.cs`:

```csharp
using Ferret.Cli.Cli;
using Ferret.Cli.Commands;

namespace Ferret.Cli.Tests.Commands;

public sealed class RootCommandFactoryAdapterTests
{
    [Fact]
    public void Build_WithUnsupportedOptionType_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            RootCommandFactory.Build([new UnsupportedOptionTypeModule()]));
    }

    private sealed class UnsupportedOptionTypeModule : CliModuleBase
    {
        public override string Name => "stub.unsupported";
        public override string Description => "Stub for unsupported option type.";

        public override IEnumerable<CommandDefinition> GetCommands()
        {
            yield return new CommandDefinition(
                new CommandMetadata("cmd", "A command."),
                HandlerType: typeof(object),
                Options:
                [
                    new OptionDefinition("--value", "A double value.", typeof(double)),
                ]);
        }
    }
}
```

- [ ] **Step 2: Run the test — confirm it fails**

```
dotnet test tests/Ferret.Cli.Tests/Ferret.Cli.Tests.csproj \
  --filter "FullyQualifiedName~RootCommandFactoryAdapterTests" -v normal
```

Expected: **FAIL** — current `MakeOption` falls through to `new Option<string>` for unknown types; no exception is thrown, so `Assert.Throws` fails with "No exception was thrown".

- [ ] **Step 3: Replace the two type-switch chains and add adapter infrastructure**

Replace the entire content of `src/Ferret.Cli/Commands/RootCommandFactory.cs` with:

```csharp
using System.CommandLine;

using Ferret.Cli.Cli;
using Ferret.Cli.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Cli.Commands;

/// <summary>
/// Why: The ONLY file importing System.CommandLine types (alongside GlobalOptions, ConsoleFormatter, FerretContext.From).
///      Swapping CLI frameworks = only this file changes.
///      Discovers ICliModule instances, builds DI container, constructs command tree.
/// Thread Safety: Single Thread Only — called once at startup.
/// </summary>
internal static class RootCommandFactory
{
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

    /// <summary>Builds the root command from the given modules.</summary>
    /// <param name="modules">The CLI modules to register.</param>
    /// <param name="output">Optional TextWriter override; defaults to Console.Out.</param>
    /// <returns>A <see cref="FerretApp"/> that can invoke the CLI.</returns>
    internal static FerretApp Build(IEnumerable<ICliModule> modules, TextWriter? output = null)
    {
        var moduleList = modules.ToList();

        IConfiguration config = FerretConfigLoader.Load(null);

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddLogging(b => b.ClearProviders());
        foreach (var module in moduleList)
        {
            module.ConfigureServices(services);
        }

        var provider = services.BuildServiceProvider();

        var root = new RootCommand("Ferret — Dig Deep. Deliver Context.");
        GlobalOptions.AddAll(root);

        var allDefs = moduleList.SelectMany(m => m.GetCommands()).ToList();
        var grouped = allDefs
            .Where(d => d.Group is not null)
            .GroupBy(d => d.Group!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

        foreach (var def in allDefs.Where(d => d.Group is null))
        {
            var cmd = BuildCommand(def, provider, config, output);
            if (grouped.TryGetValue(def.Metadata.Name, out var subDefs))
            {
                foreach (var subDef in subDefs)
                {
                    cmd.Add(BuildCommand(subDef, provider, config, output));
                }
            }

            root.Add(cmd);
        }

        return new FerretApp(root, output);
    }

    private static Command BuildCommand(
        CommandDefinition def,
        IServiceProvider provider,
        IConfiguration config,
        TextWriter? output)
    {
        var cmd = new Command(def.Metadata.Name, def.Metadata.Description);
        if (def.Metadata.Hidden)
        {
            cmd.Hidden = true;
        }

        var optMap = new Dictionary<string, Option>(StringComparer.Ordinal);
        foreach (var optDef in def.Options ?? [])
        {
            var opt = MakeOption(optDef);
            cmd.Add(opt);
            optMap[optDef.LongName.TrimStart('-')] = opt;
        }

        var argMap = new Dictionary<string, Argument<string>>(StringComparer.Ordinal);
        foreach (var argDef in def.Arguments ?? [])
        {
            var arg = new Argument<string>(argDef.Name) { Description = argDef.Description };
            if (!argDef.IsRequired)
            {
                arg.Arity = ArgumentArity.ZeroOrOne;
            }

            cmd.Add(arg);
            argMap[argDef.Name] = arg;
        }

        if (def.HandlerType is null)
        {
            if (def.PlannedSubcommands is { Count: > 0 })
            {
                RegisterGroupStubAction(cmd, def, output);
            }
        }
        else
        {
            RegisterHandlerAction(cmd, def.HandlerType, provider, config, optMap, argMap, output);
        }

        return cmd;
    }

    private static void RegisterGroupStubAction(Command cmd, CommandDefinition def, TextWriter? output)
    {
        var planned = def.PlannedSubcommands ?? [];
        var sprint = def.PlannedSprint ?? "A future sprint";
        var description = def.Metadata.Description;

        cmd.SetAction((ParseResult _) =>
        {
            var writer = output ?? Console.Out;
            writer.WriteLine(description);
            writer.WriteLine();
            writer.WriteLine("No commands are currently installed.");
            writer.WriteLine();
            writer.WriteLine($"{sprint} will introduce:");
            foreach (var sub in planned)
            {
                writer.WriteLine($"  {sub}");
            }
        });
    }

    private static void RegisterHandlerAction(
        Command cmd,
        Type handlerType,
        IServiceProvider provider,
        IConfiguration config,
        Dictionary<string, Option> optMap,
        Dictionary<string, Argument<string>> argMap,
        TextWriter? output)
    {
        cmd.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            var writer = output ?? Console.Out;
            var formatter = new ConsoleFormatter(writer, VerbosityLevel.Normal);
            var ferretServices = new FerretServices(
                provider,
                config,
                NullLoggerFactory.Instance,
                formatter);
            var parsedOpts = ParseOptions(parseResult, optMap);

            // Merge positional arguments into the options dict so GetOption<T>("name") works for both.
            foreach (var (name, arg) in argMap)
            {
                parsedOpts[name] = parseResult.GetValue(arg);
            }

            var context = FerretContext.From(parseResult, ferretServices, parsedOpts, ct);
            var handler = (ICommandHandler)provider.GetRequiredService(handlerType);
            return (int)await handler.ExecuteAsync(context).ConfigureAwait(false);
        });
    }

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

    private static Dictionary<string, object?> ParseOptions(
        ParseResult parseResult,
        Dictionary<string, Option> optMap)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (name, opt) in optMap)
            result[name] = Adapters[opt.ValueType].ExtractValue(opt, parseResult);
        return result;
    }

    private interface IOptionAdapter
    {
        Option CreateOption(OptionDefinition def);
        object? ExtractValue(Option opt, ParseResult parseResult);
    }

    private abstract class OptionAdapter<T> : IOptionAdapter
    {
        public abstract Option<T> CreateOption(OptionDefinition def);

        Option IOptionAdapter.CreateOption(OptionDefinition def) => CreateOption(def);

        public object? ExtractValue(Option opt, ParseResult parseResult) =>
            parseResult.GetValue((Option<T>)opt);
    }

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
}
```

- [ ] **Step 4: Run the new test — confirm it passes**

```
dotnet test tests/Ferret.Cli.Tests/Ferret.Cli.Tests.csproj \
  --filter "FullyQualifiedName~RootCommandFactoryAdapterTests" -v normal
```

Expected: **PASS** — `MakeOption` now throws `NotSupportedException` for `typeof(double)`.

- [ ] **Step 5: Run the full Ferret.Cli.Tests suite — confirm no regressions**

```
dotnet test tests/Ferret.Cli.Tests/Ferret.Cli.Tests.csproj -v normal
```

Expected: **All tests pass.** The grouping tests, option tests, and integration tests exercise the public behavior that the refactor preserves unchanged.

- [ ] **Step 6: Commit**

```
git add src/Ferret.Cli/Commands/RootCommandFactory.cs \
        tests/Ferret.Cli.Tests/Commands/RootCommandFactoryAdapterTests.cs
git commit -m "refactor(cli): pluggable option adapters in RootCommandFactory

Replace MakeOption/ParseOptions type-switch chains with an internal
IOptionAdapter registry. Each adapter owns Option<T> creation and value
extraction; OptionAdapter<T> base class supplies ExtractValue once via
generic cast. Adds NotSupportedException on unregistered types (fail fast)
and one regression test locking in that invariant."
```
