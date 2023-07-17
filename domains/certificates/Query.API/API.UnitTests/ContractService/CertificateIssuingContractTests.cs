using System;
using API.ContractService;
using CertificateValueObjects;
using FluentAssertions;
using Xunit;

namespace API.UnitTests.ContractService;

public class CertificateIssuingContractTests
{
    /// <summary>
    /// Contract that runs from 12:00 to 13:00 on Jan 1st 2023
    /// </summary>
    private readonly CertificateIssuingContract contract = new()
    {
        Id = Guid.NewGuid(),
        GSRN = "gsrn",
        GridArea = "gridArea",
        MeteringPointType = MeteringPointType.Production,
        MeteringPointOwner = "owner",
        StartDate = DateTimeOffset.Parse("2023-01-01T12:00:00Z"),
        EndDate = DateTimeOffset.Parse("2023-01-01T13:00:00Z"),
        Created = DateTimeOffset.Parse("2023-01-01T12:00:00Z")
    };

    [Theory]
    [InlineData("2023-01-01T12:00:00Z", "2023-01-01T13:00:00Z")]
    [InlineData("2023-01-01T11:00:00Z", "2023-01-01T12:00:01Z")]
    [InlineData("2023-01-01T11:00:00Z", "2023-01-01T12:00:00.0001Z")]
    [InlineData("2023-01-01T12:59:59Z", "2023-01-01T14:00:00Z")]
    [InlineData("2023-01-01T12:59:59.999Z", "2023-01-01T14:00:00Z")]
    public void has_overlap(string start, string end) =>
        contract.Overlaps(DateTimeOffset.Parse(start), DateTimeOffset.Parse(end))
            .Should().BeTrue();

    [Theory]
    [InlineData("2023-01-01T02:00:00Z", "2023-01-01T03:00:00Z")]
    [InlineData("2023-01-01T22:00:00Z", "2023-01-01T23:00:00Z")]
    [InlineData("2023-01-01T11:00:00Z", "2023-01-01T12:00:00Z")]
    [InlineData("2023-01-01T13:00:00Z", "2023-01-01T14:00:00Z")]
    public void has_no_overlap(string start, string end) =>
        contract.Overlaps(DateTimeOffset.Parse(start), DateTimeOffset.Parse(end))
            .Should().BeFalse();

    [Fact]
    public void overlaps_works_with_unix_time_seconds() =>
        contract.Overlaps(DateTimeOffset.Parse("2023-01-01T12:00:00Z").ToUnixTimeSeconds(), DateTimeOffset.Parse("2023-01-01T13:00:00Z").ToUnixTimeSeconds())
            .Should().BeTrue();

    [Theory]
    [InlineData("2023-01-01T12:00:00Z", "2023-01-01T13:00:00Z")]
    [InlineData("2023-01-01T12:15:00Z", "2023-01-01T12:45:00Z")]
    [InlineData("2023-01-01T12:00:00Z", "2023-01-01T12:00:01Z")]
    [InlineData("2023-01-01T12:59:59Z", "2023-01-01T13:00:00Z")]
    public void does_contain(string start, string end) =>
        contract.Contains(DateTimeOffset.Parse(start), DateTimeOffset.Parse(end))
            .Should().BeTrue();

    [Theory]
    [InlineData("2023-01-01T11:00:00Z", "2023-01-01T12:00:00Z")]
    [InlineData("2023-01-01T13:00:00Z", "2023-01-01T14:00:00Z")]
    [InlineData("2023-01-01T11:59:59.999Z", "2023-01-01T12:30:00Z")]
    [InlineData("2023-01-01T12:30:00Z", "2023-01-01T13:00:00.0001Z")]
    public void does_not_contain(string start, string end) =>
        contract.Contains(DateTimeOffset.Parse(start), DateTimeOffset.Parse(end))
            .Should().BeFalse();

    [Fact]
    public void contains_works_with_unix_time_seconds() =>
        contract.Contains(DateTimeOffset.Parse("2023-01-01T12:00:00Z").ToUnixTimeSeconds(), DateTimeOffset.Parse("2023-01-01T13:00:00Z").ToUnixTimeSeconds())
            .Should().BeTrue();
}
