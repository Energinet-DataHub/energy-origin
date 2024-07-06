using API.Models;
using FluentAssertions;

namespace API.UnitTests.Models;

public class TermsTests
{
    [Fact]
    public void Create_ShouldReturnNewTermsInstance()
    {
        string version = "1.0";

        var terms = Terms.Create(version);

        terms.Should().NotBeNull();
        terms.Should().BeOfType<Terms>();
    }

    [Fact]
    public void Create_ShouldSetVersion()
    {
        string version = "2.1";

        var terms = Terms.Create(version);

        terms.Version.Should().Be(version);
    }

    [Fact]
    public void Create_ShouldGenerateNewId()
    {
        string version = "1.5";

        var terms = Terms.Create(version);

        terms.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_ShouldCreateUniqueInstances()
    {
        string version = "3.0";

        var terms1 = Terms.Create(version);
        var terms2 = Terms.Create(version);

        terms1.Id.Should().NotBe(terms2.Id);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("2.0.1")]
    [InlineData("v3.5")]
    public void Create_ShouldAcceptDifferentVersionFormats(string version)
    {
        var terms = Terms.Create(version);

        terms.Version.Should().Be(version);
    }
}
