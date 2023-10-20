using System;
using API.Cvr.Api.Models;
using FluentAssertions;
using Xunit;

namespace API.UnitTests.Cvr.Api.Models;

public class CvrNumberTests
{
    [Theory]
    [InlineData("12345678")]
    [InlineData("00005678")]
    [InlineData("12340000")]
    public void Success(string cvrNumber)
    {
        var cvr = new CvrNumber(cvrNumber);

        cvr.Value.Should().Be(cvrNumber);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("1234567")]
    [InlineData("123456789")]
    public void Fail(string cvrNumber)
    {
        var act = () => new CvrNumber(cvrNumber);

        act.Should().Throw<ArgumentException>();
    }
}
