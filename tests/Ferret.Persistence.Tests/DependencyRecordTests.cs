using Ferret.Core.Connectors;

using Xunit;

namespace Ferret.Persistence.Tests;

public sealed class DependencyRecordTests
{
    [Fact]
    public void Records_With_Identical_Values_Are_Equal()
    {
        var fingerprint = AssetFingerprint.CreateLightweight(new DateTimeOffset(2026, 6, 28, 12, 0, 0, TimeSpan.Zero), 1024);
        var a = new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/README.md",
            SourceFingerprint = fingerprint,
            PlainText = "content",
        };
        var b = new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/README.md",
            SourceFingerprint = fingerprint,
            PlainText = "content",
        };

        Assert.Equal(a, b);
    }

    [Fact]
    public void Records_With_Different_RequestPath_Are_Not_Equal()
    {
        var fingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 100);
        var a = new DependencyRecord { EngineResponsibility = "ParseFile", RequestPath = "/repo/a.md", SourceFingerprint = fingerprint };
        var b = new DependencyRecord { EngineResponsibility = "ParseFile", RequestPath = "/repo/b.md", SourceFingerprint = fingerprint };

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Records_With_Different_SourceFingerprint_Are_Not_Equal()
    {
        var path = "/repo/a.md";
        var a = new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = path,
            SourceFingerprint = AssetFingerprint.CreateLightweight(new DateTimeOffset(2026, 6, 28, 12, 0, 0, TimeSpan.Zero), 100),
        };
        var b = new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = path,
            SourceFingerprint = AssetFingerprint.CreateLightweight(new DateTimeOffset(2026, 6, 28, 12, 0, 0, TimeSpan.Zero), 200),
        };

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void PlainText_Is_Optional()
    {
        var record = new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        };

        Assert.Null(record.PlainText);
    }

    [Fact]
    public void ConfigurationDependency_Is_Optional()
    {
        var record = new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        };

        Assert.Null(record.ConfigurationDependency);
    }

    [Fact]
    public void Records_With_Different_ConfigurationDependency_Are_Not_Equal()
    {
        var fingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1);
        var a = new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = fingerprint,
            ConfigurationDependency = new ConfigurationDependency
            {
                Parser = new ComponentRegistrationIdentity { Id = "text/plain", Version = "1.0" },
            },
        };
        var b = new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = fingerprint,
            ConfigurationDependency = new ConfigurationDependency
            {
                Parser = new ComponentRegistrationIdentity { Id = "text/plain", Version = "2.0" },
            },
        };

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void DependencyChain_Defaults_To_Empty()
    {
        var record = new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        };

        Assert.Equal(DependencyChain.Empty, record.DependencyChain);
    }

    [Fact]
    public void Records_With_Identical_DependencyChain_Content_In_Different_List_Instances_Are_Equal()
    {
        var fingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1);
        var reference = new DependencyReference { EngineResponsibility = "SearchIndex", RequestPath = "search:/repo query" };
        var a = new DependencyRecord
        {
            EngineResponsibility = "AssembleContext",
            RequestPath = "/repo",
            SourceFingerprint = fingerprint,
            DependencyChain = new DependencyChain { References = [reference] },
        };
        var b = new DependencyRecord
        {
            EngineResponsibility = "AssembleContext",
            RequestPath = "/repo",
            SourceFingerprint = fingerprint,
            DependencyChain = new DependencyChain { References = [new DependencyReference { EngineResponsibility = "SearchIndex", RequestPath = "search:/repo query" }] },
        };

        Assert.Equal(a, b);
    }

    [Fact]
    public void Records_With_Different_DependencyChain_Are_Not_Equal()
    {
        var fingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1);
        var a = new DependencyRecord
        {
            EngineResponsibility = "AssembleContext",
            RequestPath = "/repo",
            SourceFingerprint = fingerprint,
            DependencyChain = new DependencyChain
            {
                References = [new DependencyReference { EngineResponsibility = "SearchIndex", RequestPath = "search:/repo query-a" }],
            },
        };
        var b = new DependencyRecord
        {
            EngineResponsibility = "AssembleContext",
            RequestPath = "/repo",
            SourceFingerprint = fingerprint,
            DependencyChain = new DependencyChain
            {
                References = [new DependencyReference { EngineResponsibility = "SearchIndex", RequestPath = "search:/repo query-b" }],
            },
        };

        Assert.NotEqual(a, b);
    }
}
