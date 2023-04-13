using System;
using System.Threading.Tasks;
using AggregateRepositories;
using API.AppTests.Infrastructure.Attributes;
using API.AppTests.Infrastructure.Testcontainers;
using API.Query.API.Projections;
using API.Query.API.Projections.Views;
using CertificateEvents.Aggregates;
using CertificateEvents.Primitives;
using FluentAssertions;
using Marten;
using VerifyXunit;
using Xunit;

namespace API.AppTests;

[WriteToConsole]
[UsesVerify]
public sealed class CertificatesByOwnerProjectionTest : IClassFixture<MartenDbContainer>, IDisposable
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

        var productionCertificate = CreateProductionCertificate(source);
        productionCertificate.Issue();
        await repository.Save(productionCertificate);

        var certificate = await repository.Get(productionCertificate.Id);
        certificate!.Transfer(source, target);
        await repository.Save(certificate);

        var targetView = session.Load<CertificatesByOwnerView>(target);
        var sourceView = session.Load<CertificatesByOwnerView>(source);

        sourceView!.Certificates.Should().BeEmpty();
        await Verifier.Verify(targetView);
    }

    [Fact]
    public async Task Apply_CertificateIsCreatedAndIssued_HasOneIssuedCertificate()
    {
        var owner = Guid.NewGuid().ToString();

        var productionCertificate = CreateProductionCertificate(owner);
        productionCertificate.Issue();
        await repository.Save(productionCertificate);

        var view = session.Load<CertificatesByOwnerView>(owner);
        await Verifier.Verify(view);
    }

    [Fact]
    public async Task Apply_SingleCertificateIsIssued_HasOneIssuedCertificate()
    {
        var owner = Guid.NewGuid().ToString();

        var productionCertificate = CreateProductionCertificate(owner);
        await repository.Save(productionCertificate);

        var certificate = await repository.Get(productionCertificate.Id);
        certificate!.Issue();
        await repository.Save(certificate);

        var view = session.Load<CertificatesByOwnerView>(owner);
        await Verifier.Verify(view);
    }

    [Fact]
    public async Task Apply_SingleCertificateIsRejected_HasOneRejectedCertificate()
    {
        var owner = Guid.NewGuid().ToString();

        var productionCertificate = CreateProductionCertificate(owner);

        await repository.Save(productionCertificate);

        var certificate = await repository.Get(productionCertificate.Id);
        certificate!.Reject("test");
        await repository.Save(certificate);

        var view = session.Load<CertificatesByOwnerView>(owner);
        await Verifier.Verify(view);
    }

    [Fact]
    public async Task Apply_TwoCreatedEvents_HasTwoCreatingCertificates()
    {
        var owner = Guid.NewGuid().ToString();

        var productionCertificate = CreateProductionCertificate(owner);
        var productionCertificate1 = CreateProductionCertificate(owner);

        await repository.Save(productionCertificate);
        await repository.Save(productionCertificate1);

        var view = session.Load<CertificatesByOwnerView>(owner);
        await Verifier.Verify(view);
    }

    private static ProductionCertificate CreateProductionCertificate(string owner) =>
        new(
            "DK1",
            new Period(123L, 123L),
            new Technology("F0123", "T0123"),
            owner,
            "gsrn",
            2L
        );

    public void Dispose() => session.Dispose();
}
