using API.Models;
using FluentAssertions;

namespace API.UnitTests.Models;

public class ServiceProviderTermsTests
{
    [Fact]
    public void Create_ShouldReturnNewServiceProviderTermsInstance()
    {
        var version = 1;

        var serviceProviderTerms = ServiceProviderTerms.Create(version);

        serviceProviderTerms.Should().NotBeNull();
        serviceProviderTerms.Should().BeOfType<ServiceProviderTerms>();
    }

    [Fact]
    public void Create_ShouldSetVersion()
    {
        var version = 2;

        var serviceProviderTerms = ServiceProviderTerms.Create(version);

        serviceProviderTerms.Version.Should().Be(version);
    }

    [Fact]
    public void Create_ShouldGenerateNewId()
    {
        var version = 1;

        var serviceProviderTerms = ServiceProviderTerms.Create(version);

        serviceProviderTerms.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_ShouldCreateUniqueInstances()
    {
        var version = 3;

        var serviceProviderTerms1 = ServiceProviderTerms.Create(version);
        var serviceProviderTerms2 = ServiceProviderTerms.Create(version);

        serviceProviderTerms1.Id.Should().NotBe(serviceProviderTerms2.Id);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Create_ShouldAcceptDifferentVersionFormats(int version)
    {
        var serviceProviderTerms = ServiceProviderTerms.Create(version);

        serviceProviderTerms.Version.Should().Be(version);
    }
}
