using System;
using API.GranularCertificateIssuer;
using CertificateEvents.Primitives;
using FluentAssertions;
using Xunit;

namespace API.UnitTests.GranularCertificateIssuer;

public class ProductionCertificateTest
{
    [Fact]
    public void can_issue_certificate()
    {
        var sut = CreateProductionCertificate();

        sut.Issue();

        sut.Version.Should().Be(2);
    }

    [Fact]
    public void cannot_issue_if_rejected()
    {
        var sut = CreateProductionCertificate();

        sut.Reject("foo");
        sut.Invoking(s => s.Issue()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void can_reject_certificate()
    {
        var sut = CreateProductionCertificate();

        sut.Reject("foo");

        sut.Version.Should().Be(2);
    }

    [Fact]
    public void cannot_reject_if_issued()
    {
        var sut = CreateProductionCertificate();

        sut.Issue();
        sut.Invoking(s => s.Reject("foo")).Should().Throw<InvalidOperationException>();

        sut.Version.Should().Be(2);
    }

    [Fact]
    public void cannot_transfer_if_not_issued()
    {
        var sut = CreateProductionCertificate();

        sut.Invoking(s => s.Transfer("owner1", "owner2")).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void can_transfer_if_issued()
    {
        var sut = CreateProductionCertificate();

        sut.Issue();
        sut.Transfer("owner1", "owner2");

        sut.CertificateOwner.Should().Be("owner2");
    }

    [Fact]
    public void can_transfer_back_to_original_owner()
    {
        var sut = CreateProductionCertificate();

        sut.Issue();
        sut.Transfer("owner1", "owner2");
        sut.Transfer("owner2", "owner1");

        sut.CertificateOwner.Should().Be("owner1");
    }

    [Fact]
    public void cannot_transfer_to_same_owner()
    {
        var sut = CreateProductionCertificate();

        sut.Issue();
        sut.Invoking(s => s.Transfer("owner1", "owner1")).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void cannot_transfer_from_wrong_owner()
    {
        var sut = CreateProductionCertificate();

        sut.Issue();
        sut.Invoking(s => s.Transfer("wrong-owner", "owner2")).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void has_a_parameterless_constructor()
    {
        var sut = Activator.CreateInstance(typeof(ProductionCertificate), true);
        sut.Should().NotBeNull();
    }

    private static ProductionCertificate CreateProductionCertificate() =>
        new(
            "gridArea",
            new Period(1, 42),
            new Technology(FuelCode: "F00000000", TechCode: "T010000"),
            "owner1",
            "gsrn",
            42);
}
