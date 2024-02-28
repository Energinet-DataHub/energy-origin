using Xunit;
using System;
using API.MeteringPoints.Api.Dto.Responses;
using API.MeteringPoints.Api.Dto.Responses.Enums;
using FluentAssertions;

namespace Tests.Models;

public class MeteringPointTests
{
    [Theory]
    [InlineData("E17", MeterType.Consumption)]
    [InlineData("E18", MeterType.Production)]
    [InlineData("D01", MeterType.Child)]
    [InlineData("D99", MeterType.Child)]
    public void MeteringPoints_MeterType_Success(string stringType, MeterType targetType)
    {
        var mt = MeteringPoint.GetMeterType(stringType);

        Assert.Equal(targetType, mt);
    }

    [Fact]
    public void MeteringPoints_MapToChildMeterType_Success()
    {
        for (var i = 1; i < 100; i++)
        {
            var mtStr = $"D{i.ToString("00")}";

            var mt = MeteringPoint.GetMeterType(mtStr);

            Assert.Equal(MeterType.Child, mt);
        }
    }

    [Theory]
    [InlineData("D00")]
    [InlineData("D100")]
    [InlineData("F12")]
    public void MeteringPoints_MeterType_Exception(string stringType)
    {
        var sut = () => MeteringPoint.GetMeterType(stringType);

        sut.Should().Throw<NotSupportedException>();
    }

    [Theory]
    [InlineData("1000", "DK2")]
    [InlineData("4500", "DK2")]
    [InlineData("5000", "DK1")]
    [InlineData("7000", "DK1")]
    [InlineData("9000", "DK1")]
    public void MeteringPoints_GridArea_Success(string postcode, string gridArea)
    {
        var ga = MeteringPoint.GetGridArea(postcode);

        Assert.Equal(gridArea, ga);
    }

    [Theory]
    [InlineData("D11", "E17", true)]
    [InlineData("D12", "E17", true)]
    [InlineData("SomethingElse", "E17", true)]
    [InlineData("", "E17", true)]
    [InlineData("D11", "E18", true)]
    [InlineData("D12", "E18", true)]
    [InlineData("SomethingElse", "E18", false)]
    [InlineData("", "E18", false)]
    public void MeteringPoint_CanBeUsedForIssuingCertificates(string assetType, string typeOfMp, bool expectedResult)
    {
        var canIssue = MeteringPoint.GetCanBeUsedForIssuingCertificates(typeOfMp, assetType);

        canIssue.Should().Be(expectedResult);
    }
}
