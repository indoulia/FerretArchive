using Ferret.Core.Events;
using Ferret.Core.Primitives;

namespace Ferret.Core.Tests.Events;

public sealed class EventBaseTests
{
    [Fact]
    public void DomainEvent_Has_EventId_And_OccurredOn()
    {
        var ev = new TestDomainEvent("agg-1", CorrelationId.Create("corr-1"));
        Assert.NotEmpty(ev.EventId);
        Assert.True(ev.OccurredOn > DateTimeOffset.MinValue);
        Assert.Equal("agg-1", ev.AggregateId);
    }

    [Fact]
    public void DomainEvent_CorrelationId_IsPreserved()
    {
        var corr = CorrelationId.Create("corr-abc");
        var ev = new TestDomainEvent("agg-1", corr);
        Assert.Equal("corr-abc", ev.CorrelationId.Value);
    }

    [Fact]
    public void IntegrationEvent_Has_Source()
    {
        var ev = new TestIntegrationEvent("module.workspace", CorrelationId.Create("c-1"));
        Assert.Equal("module.workspace", ev.Source);
        Assert.NotEmpty(ev.EventId);
    }

    [Fact]
    public void SystemEvent_Has_Component()
    {
        var ev = new TestSystemEvent("platform.boot", CorrelationId.Create("c-2"));
        Assert.Equal("platform.boot", ev.Component);
    }

    [Fact]
    public void EventEnvelope_Wraps_Event()
    {
        var ev = new TestDomainEvent("agg-1", CorrelationId.Create("c-3"));
        var envelope = new EventEnvelope(ev, "v1");
        Assert.Equal(ev, envelope.Payload);
        Assert.Equal("v1", envelope.SchemaVersion);
        Assert.NotEmpty(envelope.EnvelopeId);
    }

    [Fact]
    public void EventMetadata_Stores_Properties()
    {
        var meta = new EventMetadata("source.module", "v1");
        Assert.Equal("source.module", meta.Source);
        Assert.Equal("v1", meta.SchemaVersion);
    }

    private sealed class TestDomainEvent : DomainEvent
    {
        public TestDomainEvent(string aggregateId, CorrelationId correlationId)
            : base(aggregateId, correlationId)
        {
        }
    }

    private sealed class TestIntegrationEvent : IntegrationEvent
    {
        public TestIntegrationEvent(string source, CorrelationId correlationId)
            : base(source, correlationId)
        {
        }
    }

    private sealed class TestSystemEvent : SystemEvent
    {
        public TestSystemEvent(string component, CorrelationId correlationId)
            : base(component, correlationId)
        {
        }
    }
}
