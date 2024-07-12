using API.Models;
using FluentAssertions;

namespace API.UnitTests.Models;

public class TermsTests
{
    [Fact]
    public void Create_ShouldReturnNewTermsInstance()
    {
        var version = 1;

        var terms = Terms.Create(version);

        terms.Should().NotBeNull();
        terms.Should().BeOfType<Terms>();
    }

    [Fact]
    public void Create_ShouldSetVersion()
    {
        var version = 2;

        var terms = Terms.Create(version);

        terms.Version.Should().Be(version);
    }

    [Fact]
    public void Create_ShouldGenerateNewId()
    {
        var version = 1;

        var terms = Terms.Create(version);

        terms.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_ShouldCreateUniqueInstances()
    {
        var version = 3;

        var terms1 = Terms.Create(version);
        var terms2 = Terms.Create(version);

        terms1.Id.Should().NotBe(terms2.Id);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Create_ShouldAcceptDifferentVersionFormats(int version)
    {
        var terms = Terms.Create(version);

        terms.Version.Should().Be(version);
    }
}
