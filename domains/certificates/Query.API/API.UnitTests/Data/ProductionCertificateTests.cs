using System;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using FluentAssertions;
using Xunit;

namespace API.UnitTests.Data;

public class ProductionCertificateTests
{
    private readonly ProductionCertificate productionCertificate = new(
        "gridArea",
        new Period(1, 42),
        new Technology(FuelCode: "F00000000", TechCode: "T010000"),
        "owner1",
        "gsrn",
        42,
        Array.Empty<byte>());

    [Fact]
    public void is_initially_not_issued_or_rejected()
    {
        productionCertificate.IsIssued.Should().BeFalse();
        productionCertificate.IsRejected.Should().BeFalse();
    }

    [Fact]
    public void can_issue_certificate()
    {
        productionCertificate.Issue();

        productionCertificate.IsIssued.Should().BeTrue();
    }

    [Fact]
    public void can_reject_certificate()
    {
        productionCertificate.Reject("foo");

        productionCertificate.IsRejected.Should().BeTrue();
    }

    [Fact]
    public void cannot_issue_if_rejected()
    {
        productionCertificate.Reject("foo");

        productionCertificate.Invoking(s => s.Issue()).Should().Throw<CertificateDomainException>();
    }

    [Fact]
    public void cannot_reject_if_issued()
    {
        productionCertificate.Issue();

        productionCertificate.Invoking(s => s.Reject("foo")).Should().Throw<CertificateDomainException>();
    }

    [Fact]
    public void has_a_parameterless_constructor()
    {
        var sut = Activator.CreateInstance(typeof(ProductionCertificate), true);
        sut.Should().NotBeNull();
    }
}
