using System;
using CertificateEvents.Aggregates;
using CertificateEvents.Exceptions;
using CertificateValueObjects;
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

    //[Fact]
    //public void delete_me()
    //{
    //    var commitment1 = new SecretCommitmentInfo((uint)42);

    //    var r = commitment1.BlindingValue.ToArray();
    //    var msg = commitment1.Message;

    //    //var commitment2 = new SecretCommitmentInfo(msg, r);
    //    var commitment2 = new SecretCommitmentInfo(msg);

    //    commitment2.Message.Should().Be(42);
    //    commitment2.BlindingValue.ToArray().Should().BeEquivalentTo(commitment1.BlindingValue.ToArray());
    //    commitment2.Commitment.C.ToArray().Should().BeEquivalentTo(commitment1.Commitment.C.ToArray());
    //}

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
