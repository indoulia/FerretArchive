using Ferret.Core.Connectors;

using Xunit;

namespace Ferret.Persistence.Tests;

public sealed class ResolutionCheckTests : IDisposable
{
    private readonly List<string> _tempDirectories = [];

    [Fact]
    public void Compare_RecordedFingerprintEqualsCurrent_ReturnsSatisfied()
    {
        var fingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 100);

        var outcome = ResolutionCheck.Compare(recordReadable: true, fingerprint, fingerprint);

        Assert.Equal(ResolutionOutcome.Satisfied, outcome);
    }

    [Fact]
    public void Compare_RecordedFingerprintDiffersFromCurrent_ReturnsNotSatisfied()
    {
        var recorded = AssetFingerprint.CreateLightweight(new DateTimeOffset(2026, 6, 28, 12, 0, 0, TimeSpan.Zero), 100);
        var current = AssetFingerprint.CreateLightweight(new DateTimeOffset(2026, 6, 29, 12, 0, 0, TimeSpan.Zero), 200);

        var outcome = ResolutionCheck.Compare(recordReadable: true, recorded, current);

        Assert.Equal(ResolutionOutcome.NotSatisfied, outcome);
    }

    [Fact]
    public void Compare_RecordUnreadable_ReturnsIndeterminate()
    {
        var current = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 100);

        var outcome = ResolutionCheck.Compare(recordReadable: false, recordedFingerprint: null, current);

        Assert.Equal(ResolutionOutcome.Indeterminate, outcome);
    }

    [Theory]
    [InlineData(new[] { ResolutionOutcome.Satisfied, ResolutionOutcome.Satisfied }, ResolutionOutcome.Satisfied)]
    [InlineData(new ResolutionOutcome[0], ResolutionOutcome.Satisfied)]
    [InlineData(new[] { ResolutionOutcome.Satisfied, ResolutionOutcome.Indeterminate }, ResolutionOutcome.Indeterminate)]
    [InlineData(new[] { ResolutionOutcome.Indeterminate, ResolutionOutcome.NotSatisfied }, ResolutionOutcome.NotSatisfied)]
    [InlineData(new[] { ResolutionOutcome.NotSatisfied, ResolutionOutcome.Satisfied, ResolutionOutcome.Indeterminate }, ResolutionOutcome.NotSatisfied)]
    public void Combine_Follows_ARCH029_NotSatisfied_Then_Indeterminate_Then_Satisfied_Ordering(
        ResolutionOutcome[] outcomes, ResolutionOutcome expected)
    {
        Assert.Equal(expected, ResolutionCheck.Combine(outcomes));
    }

    [Fact]
    public void CompareConfiguration_WhenRecordedIsNull_ReturnsSatisfied()
    {
        var current = new ConfigurationDependency { Parser = new ComponentRegistrationIdentity { Id = "text/plain", Version = "1.0" } };

        var outcome = ResolutionCheck.CompareConfiguration(recorded: null, current);

        Assert.Equal(ResolutionOutcome.Satisfied, outcome);
    }

    [Fact]
    public void CompareConfiguration_WhenCurrentIsNull_ReturnsIndeterminate()
    {
        var recorded = new ConfigurationDependency { Parser = new ComponentRegistrationIdentity { Id = "text/plain", Version = "1.0" } };

        var outcome = ResolutionCheck.CompareConfiguration(recorded, current: null);

        Assert.Equal(ResolutionOutcome.Indeterminate, outcome);
    }

    [Fact]
    public void CompareConfiguration_WhenIdentitiesMatch_ReturnsSatisfied()
    {
        var recorded = new ConfigurationDependency
        {
            Parser = new ComponentRegistrationIdentity { Id = "text/plain", Version = "1.0" },
            Connector = new ComponentRegistrationIdentity { Id = "filesystem", Version = "1.0" },
        };
        var current = new ConfigurationDependency
        {
            Parser = new ComponentRegistrationIdentity { Id = "text/plain", Version = "1.0" },
            Connector = new ComponentRegistrationIdentity { Id = "filesystem", Version = "1.0" },
        };

        var outcome = ResolutionCheck.CompareConfiguration(recorded, current);

        Assert.Equal(ResolutionOutcome.Satisfied, outcome);
    }

    [Fact]
    public void CompareConfiguration_WhenParserVersionDiffers_ReturnsNotSatisfied()
    {
        var recorded = new ConfigurationDependency { Parser = new ComponentRegistrationIdentity { Id = "text/plain", Version = "1.0" } };
        var current = new ConfigurationDependency { Parser = new ComponentRegistrationIdentity { Id = "text/plain", Version = "2.0" } };

        var outcome = ResolutionCheck.CompareConfiguration(recorded, current);

        Assert.Equal(ResolutionOutcome.NotSatisfied, outcome);
    }

    [Fact]
    public void CompareConfiguration_WhenConnectorIdDiffers_ReturnsNotSatisfied()
    {
        var recorded = new ConfigurationDependency { Connector = new ComponentRegistrationIdentity { Id = "filesystem", Version = "1.0" } };
        var current = new ConfigurationDependency { Connector = new ComponentRegistrationIdentity { Id = "sharepoint", Version = "1.0" } };

        var outcome = ResolutionCheck.CompareConfiguration(recorded, current);

        Assert.Equal(ResolutionOutcome.NotSatisfied, outcome);
    }

    [Fact]
    public async Task CompareChainAsync_EmptyChain_ReturnsSatisfied()
    {
        var store = new FileDependencyStateStore(CreateTempDirectory());

        var outcome = await ResolutionCheck.CompareChainAsync(DependencyChain.Empty, store);

        Assert.Equal(ResolutionOutcome.Satisfied, outcome);
    }

    [Fact]
    public async Task CompareChainAsync_WhenReferencedRecordIsMissing_ReturnsIndeterminate_FailClosed()
    {
        var store = new FileDependencyStateStore(CreateTempDirectory());
        var chain = new DependencyChain
        {
            References = [new DependencyReference { EngineResponsibility = "SearchIndex", RequestPath = "search:/repo query" }],
        };

        var outcome = await ResolutionCheck.CompareChainAsync(chain, store);

        Assert.Equal(ResolutionOutcome.Indeterminate, outcome);
    }

    [Fact]
    public async Task CompareChainAsync_WhenEveryReferencedRecordExistsAndHasNoFurtherChain_ReturnsSatisfied()
    {
        var store = new FileDependencyStateStore(CreateTempDirectory());
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "SearchIndex",
            RequestPath = "search:/repo query",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        });
        var chain = new DependencyChain
        {
            References = [new DependencyReference { EngineResponsibility = "SearchIndex", RequestPath = "search:/repo query" }],
        };

        var outcome = await ResolutionCheck.CompareChainAsync(chain, store);

        Assert.Equal(ResolutionOutcome.Satisfied, outcome);
    }

    [Fact]
    public async Task CompareChainAsync_FollowsReferencesTransitively_ThroughMultipleLinks()
    {
        var store = new FileDependencyStateStore(CreateTempDirectory());
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "SearchIndex",
            RequestPath = "search:/repo query-leaf",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        });
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "AssembleContext",
            RequestPath = "/repo",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 2),
            DependencyChain = new DependencyChain
            {
                References = [new DependencyReference { EngineResponsibility = "SearchIndex", RequestPath = "search:/repo query-leaf" }],
            },
        });
        var chain = new DependencyChain
        {
            References = [new DependencyReference { EngineResponsibility = "AssembleContext", RequestPath = "/repo" }],
        };

        // The top-level chain only names "AssembleContext" directly; its own persisted record's
        // chain (fetched via the store, never embedded) names "SearchIndex" — proving traversal
        // follows the reference to the record and reads *that* record's own chain, transitively.
        var outcome = await ResolutionCheck.CompareChainAsync(chain, store);

        Assert.Equal(ResolutionOutcome.Satisfied, outcome);
    }

    [Fact]
    public async Task CompareChainAsync_WhenATransitiveLinkIsMissing_ReturnsIndeterminate()
    {
        var store = new FileDependencyStateStore(CreateTempDirectory());
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "AssembleContext",
            RequestPath = "/repo",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 2),
            DependencyChain = new DependencyChain
            {
                References = [new DependencyReference { EngineResponsibility = "SearchIndex", RequestPath = "search:/repo query-leaf-never-stored" }],
            },
        });
        var chain = new DependencyChain
        {
            References = [new DependencyReference { EngineResponsibility = "AssembleContext", RequestPath = "/repo" }],
        };

        var outcome = await ResolutionCheck.CompareChainAsync(chain, store);

        Assert.Equal(ResolutionOutcome.Indeterminate, outcome);
    }

    [Fact]
    public async Task CompareChainAsync_WhenAReferenceCycleExists_ReturnsIndeterminate_RatherThanRecursingForever()
    {
        var store = new FileDependencyStateStore(CreateTempDirectory());
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "A",
            RequestPath = "a",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
            DependencyChain = new DependencyChain { References = [new DependencyReference { EngineResponsibility = "B", RequestPath = "b" }] },
        });
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "B",
            RequestPath = "b",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
            DependencyChain = new DependencyChain { References = [new DependencyReference { EngineResponsibility = "A", RequestPath = "a" }] },
        });
        var chain = new DependencyChain { References = [new DependencyReference { EngineResponsibility = "A", RequestPath = "a" }] };

        var outcome = await ResolutionCheck.CompareChainAsync(chain, store);

        Assert.Equal(ResolutionOutcome.Indeterminate, outcome);
    }

    private string CreateTempDirectory()
    {
        var path = Path.Join(Path.GetTempPath(), $"ferret-resolution-check-test-{Guid.NewGuid():N}");
        _tempDirectories.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (var dir in _tempDirectories)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }
}
