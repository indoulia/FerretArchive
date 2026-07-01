using System.Text;

using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.Parsers;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Parsers.Tests;

public sealed class ParserPackModuleTests
{
    [Fact]
    public void Registers_All_Five_Parsers()
    {
        var services = new ServiceCollection();
        ParserPackModule.ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        var parsers = provider.GetServices<IContentParser>().ToList();
        Assert.Equal(5, parsers.Count); // PlainText, Markdown, Json, Csv, Pdf (Office added in Sprint 3)
    }

    [Fact]
    public async Task Dispatcher_Routes_A_Stream_To_The_Correct_Parser()
    {
        // The dispatcher is the public API; the registry is an implementation detail.
        var services = new ServiceCollection();
        ParserPackModule.ConfigureServices(services);
        var dispatcher = services.BuildServiceProvider().GetRequiredService<IParserDispatcher>();

        var asset = new AssetDescriptor
        {
            Id = AssetId.From(new Uri("filesystem:///Greeter.cs")),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("test"),
            Kind = AssetKind.File,
            CanonicalUri = new Uri("filesystem:///Greeter.cs"),
            DisplayName = "Greeter.cs",
            LastModified = DateTimeOffset.UtcNow,
            MediaType = "text/x-csharp",
        };
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("public class Greeter { }"));

        var result = await dispatcher.DispatchAsync(stream, asset);

        Assert.Equal(ParseResultKind.Success, result.Kind);
        Assert.Contains("Greeter", result.Value!.PlainText, StringComparison.Ordinal);
    }
}
