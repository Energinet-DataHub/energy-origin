using System;
using System.Threading.Tasks;
using AggregateRepositories;
using API.AppTests.Infrastructure;
using API.Query.API.Projections;
using API.Query.API.Projections.Views;
using CertificateEvents.Aggregates;
using CertificateEvents.Primitives;
using FluentAssertions;
using Marten;
using VerifyXunit;
using Xunit;

namespace API.AppTests;

[UsesVerify]
public class CertificatesByOwnerProjectionTest : IClassFixture<MartenDbContainer>
{
    private readonly ProductionCertificateRepository repository;
    private readonly IDocumentSession session;

    public CertificatesByOwnerProjectionTest(MartenDbContainer martenDbContainer)
    {
        using var store = DocumentStore.For(options =>
        {
            options.Connection(martenDbContainer.ConnectionString);
            options.Projections.Add(new CertificatesByOwnerProjection());
        });

        repository = new ProductionCertificateRepository(store);
        session = store.LightweightSession();
    }

    [Fact]
    public async Task Apply_Transferred_CertificateRemovedFromSourceToTarget()
    {
        var source = Guid.NewGuid().ToString();
        var target = Guid.NewGuid().ToString();

        var productionCertificate = new ProductionCertificate(
            "DK1",
            new Period(123L, 123L),
            new Technology("F0123", "T0123"),
            source,
            "gsrn", 2L
        );
        productionCertificate.Issue();
        await repository.Save(productionCertificate);

        var certificate = await repository.Get(productionCertificate.Id);
        certificate.Transfer(source, target);
        await repository.Save(certificate);

        var targetView = session.Load<CertificatesByOwnerView>(target);
        var sourceView = session.Load<CertificatesByOwnerView>(source);

        sourceView.Certificates.Should().BeEmpty();
        await Verifier.Verify(targetView);
    }

    [Fact]
    public async Task Apply_SingleCertificateIsIssued_HasOneIssuedCertificate()
    {
        var owner = Guid.NewGuid().ToString();

        var productionCertificate = new ProductionCertificate(
            "DK1",
            new Period(123L, 123L),
            new Technology("F0123", "T0123"),
            owner,
            "gsrn",
            2L
        );

        await repository.Save(productionCertificate);

        var certificate = await repository.Get(productionCertificate.Id);
        certificate.Issue();
        await repository.Save(certificate);

        var view = session.Load<CertificatesByOwnerView>(owner);
        await Verifier.Verify(view);
    }

    [Fact]
    public async Task Apply_SingleCertificateIsRejected_HasOneRejectedCertificate()
    {
        var owner = Guid.NewGuid().ToString();

        var productionCertificate = new ProductionCertificate(
            "DK1",
            new Period(123L, 123L),
            new Technology("F0123", "T0123"),
            owner,
            "gsrn",
            2L
        );

        await repository.Save(productionCertificate);

        var certificate = await repository.Get(productionCertificate.Id);
        certificate.Reject("test");
        await repository.Save(certificate);

        var view = session.Load<CertificatesByOwnerView>(owner);
        await Verifier.Verify(view);
    }

    [Fact]
    public async Task Apply_TwoCreatedEvents_HasTwoCreatingCertificates()
    {
        var owner = Guid.NewGuid().ToString();

        var productionCertificate = new ProductionCertificate(
            "DK1",
            new Period(123L, 123L),
            new Technology("F0123", "T0123"),
            owner,
            "gsrn",
            2L
        );
        var productionCertificate1 = new ProductionCertificate(
            "DK1",
            new Period(123L, 123L),
            new Technology("F0123", "T0123"),
            owner,
            "gsrn",
            2L
        );

        await repository.Save(productionCertificate);
        await repository.Save(productionCertificate1);

        var view = session.Load<CertificatesByOwnerView>(owner);
        await Verifier.Verify(view);
    }
}
