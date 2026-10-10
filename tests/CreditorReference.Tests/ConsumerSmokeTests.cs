namespace CreditorReference.Tests.ConsumerSmoke;

using Iso11649;

public class ConsumerSmokeTests
{
    [Fact]
    public void ClassNameResolvesFromConsumerNamespaceNamedCreditorReference()
    {
        var ok = CreditorReference.IsValid("RF18539007547034");
        Assert.True(ok);
    }
}
