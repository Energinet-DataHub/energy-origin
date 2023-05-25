using DomainCertificate.ValueObjects;
using FluentAssertions;
using Xunit;

namespace DomainCertificate.Tests.ValueObjects;

public class GsrnTests
{
    [Theory]
    [InlineData("571234567890123456")]
    public void Ctor_Success(string value)
    {
        var gsrn = new Gsrn(value);

        gsrn.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("471234567890123456")]
    [InlineData("57123456789012345")]
    [InlineData("5712345678901234567")]
    [InlineData("571234567890 123456")]
    [InlineData(" 571234567890123456")]
    [InlineData("571234567890123456 ")]
    [InlineData(null)]
    [InlineData("not a number")]
    [InlineData("42")]
    [InlineData("Some18CharsLongStr")]
    public void Ctor_Fail(string value)
    {
        var createGsrn = () => new Gsrn(value);

        createGsrn.Should().Throw<ArgumentException>();
    }
}