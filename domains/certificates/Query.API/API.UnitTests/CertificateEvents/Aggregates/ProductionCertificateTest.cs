using System;
using CertificateEvents.Aggregates;
using CertificateEvents.Exceptions;
using DomainCertificate.Primitives;
using DomainCertificate.ValueObjects;
using FluentAssertions;
using Xunit;

namespace API.UnitTests.CertificateEvents.Aggregates;

public class ProductionCertificateTest
{
    private readonly ProductionCertificate productionCertificate = new(
        "gridArea",
        new Period(1, 42),
        new Technology(FuelCode: "F00000000", TechCode: "T010000"),
        "owner1",
        "gsrn",
        42);

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
    public void cannot_transfer_if_not_issued()
        => productionCertificate.Invoking(s => s.Transfer("owner1", "owner2")).Should().Throw<CertificateDomainException>();

    [Fact]
    public void cannot_transfer_if_rejected()
    {
        productionCertificate.Reject("foo");

        productionCertificate.Invoking(s => s.Transfer("owner1", "owner2")).Should().Throw<CertificateDomainException>();
    }

    [Fact]
    public void can_transfer_if_issued()
    {
        productionCertificate.Issue();
        productionCertificate.Transfer("owner1", "owner2");

        productionCertificate.CertificateOwner.Should().Be("owner2");
    }

    [Fact]
    public void can_transfer_back_to_original_owner()
    {
        productionCertificate.Issue();
        productionCertificate.Transfer("owner1", "owner2");
        productionCertificate.Transfer("owner2", "owner1");

        productionCertificate.CertificateOwner.Should().Be("owner1");
    }

    [Fact]
    public void cannot_transfer_to_same_owner()
    {
        productionCertificate.Issue();

        productionCertificate.Invoking(s => s.Transfer("owner1", "owner1")).Should().Throw<CertificateDomainException>();
    }

    [Fact]
    public void cannot_transfer_from_wrong_owner()
    {
        productionCertificate.Issue();

        productionCertificate.Invoking(s => s.Transfer("wrong-owner", "owner2")).Should().Throw<CertificateDomainException>();
    }

    [Fact]
    public void has_a_parameterless_constructor()
    {
        var sut = Activator.CreateInstance(typeof(ProductionCertificate), true);
        sut.Should().NotBeNull();
    }
}
