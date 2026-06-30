using Ferret.Core.Documents;
using Xunit;

namespace Ferret.Core.Tests.Documents;

public sealed class ParserCapabilityTests
{
    [Fact]
    public void PlainTextExtraction_Singleton_Is_Referentially_Stable()
    {
        Assert.Same(ParserCapabilities.PlainTextExtraction, ParserCapabilities.PlainTextExtraction);
    }

    [Fact]
    public void SectionExtraction_Is_In_All()
    {
        Assert.Contains(ParserCapabilities.SectionExtraction, ParserCapabilities.All);
    }

    [Fact]
    public void All_Has_Four_Entries()
    {
        Assert.Equal(4, ParserCapabilities.All.Count);
    }

    [Fact]
    public void ParserCapability_Equality_By_All_Fields()
    {
        var a = new ParserCapability("plain-text", "Plain Text Extraction", "1.0", "desc");
        var b = new ParserCapability("plain-text", "Plain Text Extraction", "1.0", "desc");
        Assert.Equal(a, b);
    }

    [Fact]
    public void ParserCapability_Inequality_Different_Id()
    {
        Assert.NotEqual(
            new ParserCapability("a", "A", "1.0", "x"),
            new ParserCapability("b", "B", "1.0", "x"));
    }
}
