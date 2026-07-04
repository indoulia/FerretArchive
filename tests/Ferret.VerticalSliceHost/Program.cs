using Ferret.Cli.Commands;
using Ferret.Persistence;
using Ferret.VerticalSlice;

if (args.Length != 4)
{
    await Console.Error.WriteLineAsync("usage: <scan-and-persist|resolve-and-reuse|cli-resolve> <rootPath> <fileName> <storePath>").ConfigureAwait(false);
    return 1;
}

var mode = args[0];
var rootPath = args[1];
var fileName = args[2];
var storePath = args[3];
var store = new FileDependencyStateStore(storePath);

switch (mode)
{
    case "scan-and-persist":
        await VerticalSliceDriver.ScanAndPersistAsync(rootPath, fileName, store).ConfigureAwait(false);
        return 0;
    case "resolve-and-reuse":
        var outcome = await VerticalSliceDriver.ResolveAndReuseAsync(rootPath, fileName, store).ConfigureAwait(false);
        await Console.Out.WriteLineAsync(outcome.ToString()).ConfigureAwait(false);
        return 0;
    case "cli-resolve":
        var filePath = Path.Join(rootPath, fileName);
        using (var cliOutput = new StringWriter())
        {
            var app = RootCommandFactory.Build([new CoreCliModule(), new VerticalSliceCliModule(storePath)], cliOutput);
            var exitCode = await app.InvokeAsync(["vslice-resolve", filePath]).ConfigureAwait(false);
            await Console.Out.WriteAsync(cliOutput.ToString()).ConfigureAwait(false);
            return exitCode;
        }

    default:
        await Console.Error.WriteLineAsync($"unknown mode: {mode}").ConfigureAwait(false);
        return 1;
}
