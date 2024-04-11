using API.IntegrationTests.Extensions;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Testing.Helpers;
using Testing.Testcontainers;
using Xunit;

namespace API.IntegrationTests.Repositories;

public class ProductionCertificateDatabaseTests : IClassFixture<PostgresContainer>, IDisposable
{
    private readonly DbContextOptions<TransferDbContext> options;

    public ProductionCertificateDatabaseTests(PostgresContainer dbContainer)
    {
        options = new DbContextOptionsBuilder<TransferDbContext>().UseNpgsql(dbContainer.ConnectionString).Options;
        using var dbContext = new TransferDbContext(options);
        dbContext.Database.EnsureCreated();
    }

    [Fact]
    public void can_save_and_update()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        var productionCertificate = new ProductionCertificate(
            gridArea: "dk1",
            period: new Period(42, 420),
            technology: new Technology("fuel", "tech"),
            meteringPointOwner: "owner1",
            gsrn: gsrn,
            quantity: 42,
            blindingValue: new byte[] { 1, 2, 3 });

        using (var dbContext = new TransferDbContext(options))
        {
            dbContext.Update(productionCertificate);
            dbContext.SaveChanges();
        }

        var id = productionCertificate.Id;

        using (var dbContext = new TransferDbContext(options))
        {
            var fetched = dbContext.ProductionCertificates.Find(id)!;

            fetched.Should().BeEquivalentTo(productionCertificate);

            fetched.Issue();

            dbContext.Update(fetched);
            dbContext.SaveChanges();
        }

        using (var dbContext = new TransferDbContext(options))
        {
            var fetchedAfterIssued = dbContext.ProductionCertificates.Find(id)!;
            fetchedAfterIssued.IsIssued.Should().BeTrue();
        }
    }

    [Fact]
    public void cannot_create_certificates_with_same_period_and_gsrn()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        var period = new Period(dateFrom: 42, dateTo: 420);

        var productionCertificate1 = new ProductionCertificate(
            gridArea: "dk1",
            period: period,
            technology: new Technology(FuelCode: "fuel", TechCode: "tech"),
            meteringPointOwner: "owner1",
            gsrn: gsrn,
            quantity: 42,
            blindingValue: new byte[] { 1, 2, 3 });

        var productionCertificate2 = new ProductionCertificate(
            gridArea: "dk1",
            period: period,
            technology: new Technology(FuelCode: "fuel", TechCode: "tech"),
            meteringPointOwner: "owner1",
            gsrn: gsrn,
            quantity: 42,
            blindingValue: new byte[] { 1, 2, 3 });

        using (var dbContext = new TransferDbContext(options))
        {
            dbContext.Update(productionCertificate1);
            dbContext.SaveChanges();
        }

        using (var dbContext = new TransferDbContext(options))
        {
            dbContext.Update(productionCertificate2);
            Action act = () => dbContext.SaveChanges();
            act.Should().Throw<DbUpdateException>();
        }

        using (var dbContext = new TransferDbContext(options))
        {
            var certificates = dbContext.ProductionCertificates.ToList();
            certificates.Should().BeEquivalentTo(new[] { productionCertificate1 });
        }
    }

    [Fact]
    public void can_create_certificates_with_same_period_but_different_gsrns()
    {
        var gsrn1 = GsrnHelper.GenerateRandom();
        var gsrn2 = GsrnHelper.GenerateRandom();
        var period = new Period(dateFrom: 42, dateTo: 420);

        var productionCertificate1 = new ProductionCertificate(
            gridArea: "dk1",
            period: period,
            technology: new Technology(FuelCode: "fuel", TechCode: "tech"),
            meteringPointOwner: "owner1",
            gsrn: gsrn1,
            quantity: 42,
            blindingValue: new byte[] { 1, 2, 3 });

        var productionCertificate2 = new ProductionCertificate(
            gridArea: "dk1",
            period: period,
            technology: new Technology(FuelCode: "fuel", TechCode: "tech"),
            meteringPointOwner: "owner1",
            gsrn: gsrn2,
            quantity: 42,
            blindingValue: new byte[] { 1, 2, 3 });

        using (var dbContext = new TransferDbContext(options))
        {
            dbContext.Update(productionCertificate1);
            dbContext.SaveChanges();
        }

        using (var dbContext = new TransferDbContext(options))
        {
            dbContext.Update(productionCertificate2);
            dbContext.SaveChanges();
        }

        using (var dbContext = new TransferDbContext(options))
        {
            var certificates = dbContext.ProductionCertificates.ToList();
            certificates.Should().BeEquivalentTo(new[] { productionCertificate1, productionCertificate2 });
        }
    }

    [Fact]
    public void can_create_certificates_with_different_periods_but_same_gsrn()
    {
        var gsrn1 = GsrnHelper.GenerateRandom();
        var gsrn2 = GsrnHelper.GenerateRandom();
        var period = new Period(dateFrom: 42, dateTo: 420);

        var productionCertificate1 = new ProductionCertificate(
            gridArea: "dk1",
            period: period,
            technology: new Technology(FuelCode: "fuel", TechCode: "tech"),
            meteringPointOwner: "owner1",
            gsrn: gsrn1,
            quantity: 42,
            blindingValue: new byte[] { 1, 2, 3 });

        var productionCertificate2 = new ProductionCertificate(
            gridArea: "dk1",
            period: period,
            technology: new Technology(FuelCode: "fuel", TechCode: "tech"),
            meteringPointOwner: "owner1",
            gsrn: gsrn2,
            quantity: 42,
            blindingValue: new byte[] { 1, 2, 3 });

        using (var dbContext = new TransferDbContext(options))
        {
            dbContext.Update(productionCertificate1);
            dbContext.SaveChanges();
        }

        using (var dbContext = new TransferDbContext(options))
        {
            dbContext.Update(productionCertificate2);
            dbContext.SaveChanges();
        }

        using (var dbContext = new TransferDbContext(options))
        {
            var certificates = dbContext.ProductionCertificates.ToList();
            certificates.Should().BeEquivalentTo(new[] { productionCertificate1, productionCertificate2 });
        }
    }

    public void Dispose()
    {
        using var dbContext = new TransferDbContext(options);
        dbContext.RemoveAll(d => d.ProductionCertificates);

        GC.SuppressFinalize(this);
    }
}
