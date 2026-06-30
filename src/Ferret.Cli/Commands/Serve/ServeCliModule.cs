using Ferret.Cli.Cli;
using Ferret.Core.Indexing;
using Ferret.Core.Search;
using Ferret.Core.Workspace;
using Ferret.Indexing;
using Ferret.Mcp;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ferret.Cli.Commands.Serve;

/// <summary>Registers the <c>ferret serve</c> command and all MCP services.</summary>
internal sealed class ServeCliModule : CliModuleBase
{
    /// <inheritdoc/>
    public override string Name => "ferret.serve";

    /// <inheritdoc/>
    public override string Description => "MCP stdio server.";

    /// <inheritdoc/>
    public override IEnumerable<CommandDefinition> GetCommands()
    {
        yield return new CommandDefinition(
            new CommandMetadata("serve", "Expose Ferret capabilities over the MCP stdio protocol."),
            typeof(ServeCommandHandler));
    }

    /// <inheritdoc/>
    public override void ConfigureServices(IServiceCollection services)
    {
        services.TryAddSingleton<IDocumentService>(sp =>
        {
            var wc = sp.GetRequiredService<IWorkspaceContext>();
            var dbPath = System.IO.Path.Join(
                wc.WorkspaceRoot.FullPath,
                WorkspaceLayout.RootDirectoryName,
                IndexLayout.IndexDirectoryName,
                IndexLayout.KeywordDirectoryName,
                IndexLayout.KeywordDatabaseFileName);
            return new DocumentService(dbPath);
        });

        McpModule.ConfigureServices(services);
        services.AddSingleton<ServeCommandHandler>();
    }
}
