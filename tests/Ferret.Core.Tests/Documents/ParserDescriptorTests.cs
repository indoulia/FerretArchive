using Ferret.Core.Documents;
using Xunit;

namespace Ferret.Core.Tests.Documents;

public sealed class ParserDescriptorTests
{
    [Fact]
    public void ParserDescriptor_Has_No_Public_Setters()
    {
        // init-only setters are public in reflection but not settable after construction;
        // distinguish them from regular public set by checking for IsExternalInit modifier.
        var isExternalInit = typeof(System.Runtime.CompilerServices.IsExternalInit);
        var props = typeof(ParserDescriptor).GetProperties();
        Assert.All(
            props,
            p =>
            {
                if (!(p.CanWrite && (p.SetMethod?.IsPublic ?? false)))
                {
                    return;
                }

                var isInitOnly = p.SetMethod!.ReturnParameter
                    .GetRequiredCustomModifiers()
                    .Contains(isExternalInit);
                Assert.True(
                    isInitOnly,
                    $"Property '{p.Name}' must not have a public set setter — ParserDescriptor is immutable (init-only is allowed)");
            });
    }

    [Fact]
    public void ParserDescriptor_Priority_Defaults_To_100()
    {
        var desc = MakeDescriptor("text/plain", priority: 100);
        Assert.Equal(100, desc.Priority);
    }

    [Fact]
    public void ParserDescriptor_Supports_Higher_Priority()
    {
        var desc = MakeDescriptor("text/markdown", priority: 200);
        Assert.Equal(200, desc.Priority);
    }

    [Fact]
    public void ParserDescriptor_SupportedMediaTypes_Not_Empty()
    {
        var desc = MakeDescriptor("text/plain");
        Assert.NotEmpty(desc.SupportedMediaTypes);
    }

    private static ParserDescriptor MakeDescriptor(string mediaType, int priority = 100) =>
        new()
        {
            Id = new ParserId(mediaType),
            Name = "Test Parser",
            Version = "1.0",
            SupportedMediaTypes = [mediaType],
            Capabilities = [ParserCapabilities.PlainTextExtraction],
            Priority = priority,
        };
}
