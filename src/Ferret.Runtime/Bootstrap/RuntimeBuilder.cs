using Ferret.Core.Abstractions;
using Ferret.Core.Runtime;
using Ferret.Runtime.Events;
using Ferret.Runtime.Health;
using Ferret.Runtime.Lifecycle;
using Ferret.Runtime.Modules;
using Ferret.Runtime.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ferret.Runtime.Bootstrap;

/// <summary>
/// Fluent builder that assembles an IRuntimeHost from registered module descriptors.
/// <para>Why: Separates host construction (module registration, dependency sorting, DI wiring) from host operation (start/stop).</para>
/// <para>Lifecycle: Transient — configure once, call Build() once. Discard after Build() returns.</para>
/// <para>Layer: Ferret.Runtime — called by the application layer or DI extension helper; not referenced by Core.</para>
/// <para>Thread Safety: Single Thread Only — configure from one thread before calling Build().</para>
/// </summary>
public sealed class RuntimeBuilder : IRuntimeBuilder
{
    private readonly ModuleDescriptorStore _store = new();
    private readonly List<IHealthCheck> _extraHealthChecks = [];
    private RuntimeOptions _options = new();
    private Action<ILoggingBuilder>? _loggingConfigure;

    /// <inheritdoc/>
    public IRuntimeBuilder AddModule(IModuleDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _store.Add(descriptor);
        return this;
    }

    /// <summary>Overrides default runtime options.</summary>
    /// <param name="options">The options to apply.</param>
    /// <returns>The same builder instance, to allow call chaining.</returns>
    public RuntimeBuilder WithOptions(RuntimeOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        return this;
    }

    /// <summary>Registers an additional health check beyond those contributed by modules.</summary>
    /// <param name="check">The health check to register.</param>
    /// <returns>The same builder instance, to allow call chaining.</returns>
    public RuntimeBuilder AddHealthCheck(IHealthCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        _extraHealthChecks.Add(check);
        return this;
    }

    /// <summary>Configures the logging pipeline for the internal runtime host.</summary>
    /// <param name="configure">Delegate that configures the <see cref="ILoggingBuilder"/>.</param>
    /// <returns>The same builder instance, to allow call chaining.</returns>
    public RuntimeBuilder ConfigureLogging(Action<ILoggingBuilder> configure)
    {
        _loggingConfigure = configure ?? throw new ArgumentNullException(nameof(configure));
        return this;
    }

    /// <inheritdoc/>
    public IRuntimeHost Build()
    {
        IReadOnlyList<DefaultModule> ordered = ModuleDependencyGraph.Sort(_store.GetAll());
        RuntimeOptions options = _options;
        List<IHealthCheck> extraChecks = [.._extraHealthChecks];

        IHost host = new HostBuilder()
            .ConfigureLogging(logging => { _loggingConfigure?.Invoke(logging); })
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton(options);
                services.AddSingleton<RuntimeStateManager>();
                services.AddSingleton<RuntimeEventDispatcher>();
                services.AddSingleton<LifecycleOrchestrator>();
                services.AddSingleton<RuntimeHealthService>(sp =>
                    new RuntimeHealthService(extraChecks));
                services.AddSingleton<IReadOnlyList<DefaultModule>>(_ => ordered);
                services.AddSingleton<ModuleRegistry>(
                    _ => new ModuleRegistry(ordered));
                services.AddHostedService<ModuleLifecycleService>();
            })
            .Build();

        return new RuntimeHost(host);
    }
}
