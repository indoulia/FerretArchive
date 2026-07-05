using Ferret.Cli.Commands;
using Ferret.Cli.Commands.Config;
using Ferret.Cli.Commands.Connector;
using Ferret.Cli.Commands.Context;
using Ferret.Cli.Commands.Indexing;
using Ferret.Cli.Commands.Manual;
using Ferret.Cli.Commands.Models;
using Ferret.Cli.Commands.Prompt;
using Ferret.Cli.Commands.Serve;
using Ferret.Cli.Commands.Watch;
using Ferret.Cli.Commands.Workspace;
using Ferret.Cli.Commands.Workspaces;
using Ferret.Cli.Search;
using Ferret.Connectors.Filesystem;
using Ferret.Core.Primitives;
using Ferret.Core.Workspace;
using Ferret.ParserPlatform;
using Ferret.Workspace;

// Build IWorkspaceContext once from CWD — Sprint 10 will read workspace ID from workspace.json.
var workspaceRoot = WorkspacePath.Create(Directory.GetCurrentDirectory());
var workspaceId = WorkspaceId.Create("default");
IWorkspaceContext workspaceContext = new DefaultWorkspaceContext(workspaceId, workspaceRoot);

var filesystemConfig = new FilesystemConnectorConfiguration { RootPath = workspaceRoot.FullPath };
var filesystemFactory = new FilesystemConnectorFactory(filesystemConfig, new MimeTypeResolver());

return await RootCommandFactory.Build([
    new CoreCliModule(),
    new ConfigCliModule(),
    new WorkspaceCliModule(),
    new WorkspacesCliModule(),
    new ConnectorCliModule([filesystemFactory]),
    new IndexCliModule(workspaceContext),
    new WatchCliModule(workspaceContext),
    new SearchCliModule(),
    new ContextCliModule(),
    new ServeCliModule(),
    new ManualCliModule(),
    new ModelsCliModule(),
    new PromptCliModule(),
]).InvokeAsync(args).ConfigureAwait(false);
