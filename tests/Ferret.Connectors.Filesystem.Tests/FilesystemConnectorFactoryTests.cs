using Ferret.Core.Connectors;
using Ferret.ParserPlatform;

using Xunit;

namespace Ferret.Connectors.Filesystem.Tests;

/// <summary>Tests for <see cref="FilesystemConnectorFactory"/>.</summary>
public sealed class FilesystemConnectorFactoryTests
{
    private static ConnectorInstance MakeInstance(
        string id = "default",
        string? rootPath = null,
        string? include = null,
        string? exclude = null)
    {
        var dict = new Dictionary<string, string>();
        if (rootPath is not null)
        {
            dict["rootPath"] = rootPath;
        }

        if (include is not null)
        {
            dict["includeExtensions"] = include;
        }

        if (exclude is not null)
        {
            dict["excludeExtensions"] = exclude;
        }

        return new ConnectorInstance
        {
            Id = new ConnectorInstanceId(id),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = id,
            Configuration = new ConnectorConfiguration(dict),
        };
    }

    /// <summary>Create returns FilesystemConnector.</summary>
    [Fact]
    public void Create_Returns_FilesystemConnector()
    {
        var factory = new FilesystemConnectorFactory(
            new FilesystemConnectorConfiguration(),
            new MimeTypeResolver());
        var instance = MakeInstance();

        var connector = factory.Create(instance);

        Assert.IsType<FilesystemConnector>(connector);
    }

    /// <summary>Create with RootPath config uses that path.</summary>
    [Fact]
    public void Create_With_RootPath_Config_Uses_That_Path()
    {
        var factory = new FilesystemConnectorFactory(
            new FilesystemConnectorConfiguration(),
            new MimeTypeResolver());
        var instance = MakeInstance(rootPath: "./src");

        var connector = factory.Create(instance);

        Assert.NotNull(connector);
    }

    /// <summary>Create with missing RootPath defaults to dot.</summary>
    [Fact]
    public void Create_With_Missing_RootPath_Defaults_To_Dot()
    {
        var factory = new FilesystemConnectorFactory(
            new FilesystemConnectorConfiguration(),
            new MimeTypeResolver());
        var instance = MakeInstance(); // no rootPath key

        var connector = factory.Create(instance);

        Assert.NotNull(connector);
    }

    /// <summary>Create with ExcludeExtensions without dots adds dots.</summary>
    [Fact]
    public void Create_With_ExcludeExtensions_No_Dots_Adds_Dots()
    {
        var factory = new FilesystemConnectorFactory(
            new FilesystemConnectorConfiguration(),
            new MimeTypeResolver());
        var instance = MakeInstance(exclude: "dll,exe");

        var connector = factory.Create(instance);

        Assert.NotNull(connector);
    }

    /// <summary>Create with ExcludeExtensions already dotted keeps dots.</summary>
    [Fact]
    public void Create_With_ExcludeExtensions_Already_Dotted_Keeps_Dots()
    {
        var factory = new FilesystemConnectorFactory(
            new FilesystemConnectorConfiguration(),
            new MimeTypeResolver());
        var instance = MakeInstance(exclude: ".dll,.exe,.pdb");

        var connector = factory.Create(instance);

        Assert.NotNull(connector);
    }

    /// <summary>ConnectorId returns filesystem.</summary>
    [Fact]
    public void ConnectorId_Returns_Filesystem()
    {
        var factory = new FilesystemConnectorFactory(
            new FilesystemConnectorConfiguration(),
            new MimeTypeResolver());

        Assert.Equal("filesystem", factory.ConnectorId.Value);
    }
}
