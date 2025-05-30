using System;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup.Exceptions;
using FluentAssertions;
using Xunit;

namespace API.UnitTests;

public class ReportTests
{
    [Fact]
    public void Given_ValidDateRange_When_CreatingReport_Then_ReportIsCreated()
    {
        var id = Guid.NewGuid();
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var now = UnixTimestamp.Now();
        var start = now.AddDays(-30);
        var end = now.AddDays(-5);

        var report = Report.Create(id, orgId, OrganizationName.Create("Organization Name"), Tin.Create("13371337"), start, end, Language.English);

        report.Should().NotBeNull();
        report.OrganizationId.Should().Be(orgId);
        report.StartDate.Should().Be(start);
        report.EndDate.Should().Be(end);
        report.Status.Should().Be(ReportStatus.Pending);
        report.Content.Should().BeNull();
    }

    [Fact]
    public void Given_NullOrganizationId_When_CreatingReport_Then_ThrowsBusinessException()
    {
        var start = UnixTimestamp.Now().AddDays(-30);
        var end = start.AddDays(10);

        var act = () => Report.Create(Guid.NewGuid(), null!, OrganizationName.Create("Organization Name"), Tin.Create("13371337"), start, end, Language.English);

        act.Should().Throw<BusinessException>()
            .WithMessage("*organizationId*");
    }

    [Fact]
    public void Given_EndDateLessThan7DaysAfterStart_When_CreatingReport_Then_ThrowsBusinessException()
    {
        var now = UnixTimestamp.Now();
        var start = now.AddDays(-30);
        var end = start.AddDays(6);

        var act = () => Report.Create(Guid.NewGuid(), OrganizationId.Create(Guid.NewGuid()), OrganizationName.Create("Organization Name"), Tin.Create("13371337"), start, end, Language.English);

        act.Should().Throw<BusinessException>()
            .WithMessage("*1 week*");
    }

    [Fact]
    public void Given_EndDateMoreThanOneYearAfterStart_When_CreatingReport_Then_ThrowsBusinessException()
    {
        var now = UnixTimestamp.Now();
        var start = now.AddDays(-300);
        var end = start.AddYears(1).AddDays(1);

        var act = () => Report.Create(Guid.NewGuid(), OrganizationId.Create(Guid.NewGuid()), OrganizationName.Create("Organization Name"), Tin.Create("13371337"), start, end, Language.English);

        act.Should().Throw<BusinessException>()
            .WithMessage("EndDate cannot be in the future.");
    }

    [Fact]
    public void Given_StartDateOlderThan365DaysAndFutureEndDate_When_CreatingReport_Then_ShouldThrow()
    {
        // Arrange
        var now = UnixTimestamp.Now();
        var start = now.AddDays(-400);
        var end = now.AddDays(10);

        // Act
        var act = () => Report.Create(Guid.NewGuid(), OrganizationId.Create(Guid.NewGuid()), OrganizationName.Create("Organization Name"), Tin.Create("13371337"), start, end, Language.English);

        // Assert
        act.Should().Throw<BusinessException>().Where(e => e.Message.Contains("EndDate cannot be in the future."));
    }

    [Fact]
    public void Given_EndDateExactlyOneYearAfterStart_When_CreatingReport_Then_ReportIsCreated()
    {
        var start = UnixTimestamp.Now().AddDays(-365);
        var end = start.AddYears(1);

        var report = Report.Create(Guid.NewGuid(), OrganizationId.Create(Guid.NewGuid()), OrganizationName.Create("Organization Name"), Tin.Create("13371337"), start, end, Language.English);

        report.StartDate.Should().Be(start);
        report.EndDate.Should().Be(end);
    }

    [Fact]
    public void Given_StartDateAfterEndDate_When_CreatingReport_Then_ThrowsBusinessException()
    {
        var now = UnixTimestamp.Now();
        var start = now.AddDays(-5);
        var end = now.AddDays(-20);

        var act = () => Report.Create(Guid.NewGuid(), OrganizationId.Create(Guid.NewGuid()), OrganizationName.Create("Organization Name"), Tin.Create("13371337"), start, end, Language.English);

        act.Should().Throw<BusinessException>()
            .WithMessage("*1 week*");
    }
}
