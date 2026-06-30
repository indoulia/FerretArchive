using System.CommandLine;

using Ferret.Cli.Cli;
using Ferret.Cli.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Ferret.Cli.Commands;

/// <summary>
/// Why: The ONLY file importing System.CommandLine types (alongside GlobalOptions, ConsoleFormatter, FerretContext.From).
///      Swapping CLI frameworks = only this file changes.
///      Discovers ICliModule instances, builds DI container, constructs command tree.
/// Thread Safety: Single Thread Only — called once at startup.
/// </summary>
internal static class RootCommandFactory
{
    private static readonly BoolAdapter BoolAdapterInstance = new();
    private static readonly IntAdapter IntAdapterInstance = new();
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
        [typeof(bool)] = BoolAdapterInstance,
        [typeof(int)] = IntAdapterInstance,
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
            var logLevelValue = parseResult.GetValue(GlobalOptions.LogLevel);
            using var loggerFactory = BuildLoggerFactory(logLevelValue);
            var ferretServices = new FerretServices(
                provider,
                config,
                loggerFactory,
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

    private static ILoggerFactory BuildLoggerFactory(string? logLevel)
    {
        var level = (logLevel ?? "Information").ToUpperInvariant() switch
        {
            "TRACE" => LogLevel.Trace,
            "DEBUG" => LogLevel.Debug,
            "INFO" or "INFORMATION" => LogLevel.Information,
            "WARNING" or "WARN" => LogLevel.Warning,
            "ERROR" => LogLevel.Error,
            "CRITICAL" => LogLevel.Critical,
            _ => LogLevel.Information,
        };

        return LoggerFactory.Create(builder =>
            builder.SetMinimumLevel(level).AddConsole());
    }

    private static Option MakeOption(OptionDefinition def)
    {
        if (!Adapters.TryGetValue(def.ValueType, out var adapter))
        {
            throw new NotSupportedException(
                $"No option adapter is registered for CLR type '{def.ValueType.Name}'.\n" +
                $"Either:\n" +
                $" - register an OptionAdapter<{def.ValueType.Name}> in RootCommandFactory.Adapters, or\n" +
                $" - change the option to use a supported CLR type.");
        }

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

#pragma warning disable SA1201, SA1600, SA1611, SA1615, SA1629
    /// <summary>Adapter for creating and extracting option values of a specific type.</summary>
    private interface IOptionAdapter
    {
        /// <summary>Creates an Option from an OptionDefinition.</summary>
        Option CreateOption(OptionDefinition def);

        /// <summary>Extracts the parsed value from an Option.</summary>
        object? ExtractValue(Option opt, ParseResult parseResult);
    }

    /// <summary>Base class for typed option adapters.</summary>
    private abstract class OptionAdapter<T> : IOptionAdapter
    {
        /// <summary>Creates an Option from the definition.</summary>
        public abstract Option<T> CreateOption(OptionDefinition def);

        /// <summary>Creates a non-generic Option (for interface compliance).</summary>
        Option IOptionAdapter.CreateOption(OptionDefinition def) => CreateOption(def);

        /// <summary>Extracts the value from a parsed Option.</summary>
        public object? ExtractValue(Option opt, ParseResult parseResult) =>
            parseResult.GetValue((Option<T>)opt);
    }

    /// <summary>Adapter for bool options.</summary>
    private sealed class BoolAdapter : OptionAdapter<bool>
    {
        /// <summary>Creates an Option from the definition.</summary>
        public override Option<bool> CreateOption(OptionDefinition def) =>
            new(def.LongName) { Description = def.Description };
    }

    /// <summary>Adapter for int options.</summary>
    private sealed class IntAdapter : OptionAdapter<int>
    {
        /// <summary>Creates an Option from the definition.</summary>
        public override Option<int> CreateOption(OptionDefinition def) =>
            new(def.LongName)
            {
                Description = def.Description,
                DefaultValueFactory = _ => def.DefaultValue is int d ? d : 0,
            };
    }

    /// <summary>Adapter for string options.</summary>
    private sealed class StringAdapter : OptionAdapter<string>
    {
        /// <summary>Creates an Option from the definition.</summary>
        public override Option<string> CreateOption(OptionDefinition def) =>
            new(def.LongName) { Description = def.Description };
    }

#pragma warning restore SA1201, SA1600, SA1611, SA1615, SA1629
}
