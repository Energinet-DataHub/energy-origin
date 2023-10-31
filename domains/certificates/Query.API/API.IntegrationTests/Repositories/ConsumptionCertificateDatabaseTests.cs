using API.Data;
using API.IntegrationTests.Testcontainers;
using Microsoft.EntityFrameworkCore;
using System;
using API.IntegrationTests.Extensions;
using Xunit;
using API.IntegrationTests.Helpers;
using CertificateValueObjects;
using FluentAssertions;
using System.Linq;

namespace API.IntegrationTests.Repositories;

public class ConsumptionCertificateDatabaseTests : IClassFixture<PostgresContainer>, IDisposable
{
    private readonly DbContextOptions<ApplicationDbContext> options;

    public ConsumptionCertificateDatabaseTests(PostgresContainer dbContainer)
    {
        options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(dbContainer.ConnectionString).Options;
        using var dbContext = new ApplicationDbContext(options);
        dbContext.Database.EnsureCreated();
    }

    [Fact]
    public void can_save_and_update()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        var cert = new ConsumptionCertificate(
            gridArea: "dk1",
            period: new Period(42, 420),
            meteringPointOwner: "owner1",
            gsrn: gsrn,
            quantity: 42,
            blindingValue: new byte[] { 1, 2, 3 });

        using (var dbContext = new ApplicationDbContext(options))
        {
            dbContext.Update(cert);
            dbContext.SaveChanges();
        }

        var id = cert.Id;

        using (var dbContext = new ApplicationDbContext(options))
        {
            var fetched = dbContext.ConsumptionCertificates.Find(id)!;

            fetched.Should().BeEquivalentTo(cert);

            fetched.Issue();

            dbContext.Update(fetched);
            dbContext.SaveChanges();
        }

        using (var dbContext = new ApplicationDbContext(options))
        {
            var fetchedAfterIssued = dbContext.ConsumptionCertificates.Find(id)!;
            fetchedAfterIssued.IsIssued.Should().BeTrue();
        }
    }

    [Fact]
    public void cannot_create_certificates_with_same_period_and_gsrn()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        var period = new Period(dateFrom: 42, dateTo: 420);

        var cert1 = new ConsumptionCertificate(
            gridArea: "dk1",
            period: period,
            meteringPointOwner: "owner1",
            gsrn: gsrn,
            quantity: 42,
            blindingValue: new byte[] { 1, 2, 3 });

        var cert2 = new ConsumptionCertificate(
            gridArea: "dk1",
            period: period,
            meteringPointOwner: "owner1",
            gsrn: gsrn,
            quantity: 42,
            blindingValue: new byte[] { 1, 2, 3 });

        using (var dbContext = new ApplicationDbContext(options))
        {
            dbContext.Update(cert1);
            dbContext.SaveChanges();
        }

        using (var dbContext = new ApplicationDbContext(options))
        {
            dbContext.Update(cert2);
            Action act = () => dbContext.SaveChanges();
            act.Should().Throw<DbUpdateException>();
        }

        using (var dbContext = new ApplicationDbContext(options))
        {
            var certificates = dbContext.ConsumptionCertificates.ToList();
            certificates.Should().BeEquivalentTo(new[] { cert1 });
        }
    }

    [Fact]
    public void can_create_certificates_with_same_period_but_different_gsrns()
    {
        var gsrn1 = GsrnHelper.GenerateRandom();
        var gsrn2 = GsrnHelper.GenerateRandom();
        var period = new Period(dateFrom: 42, dateTo: 420);

        var cert1 = new ConsumptionCertificate(
            gridArea: "dk1",
            period: period,
            meteringPointOwner: "owner1",
            gsrn: gsrn1,
            quantity: 42,
            blindingValue: new byte[] { 1, 2, 3 });

        var cert2 = new ConsumptionCertificate(
            gridArea: "dk1",
            period: period,
            meteringPointOwner: "owner1",
            gsrn: gsrn2,
            quantity: 42,
            blindingValue: new byte[] { 1, 2, 3 });

        using (var dbContext = new ApplicationDbContext(options))
        {
            dbContext.Update(cert1);
            dbContext.SaveChanges();
        }

        using (var dbContext = new ApplicationDbContext(options))
        {
            dbContext.Update(cert2);
            dbContext.SaveChanges();
        }

        using (var dbContext = new ApplicationDbContext(options))
        {
            var certificates = dbContext.ConsumptionCertificates.ToList();
            certificates.Should().BeEquivalentTo(new[] { cert1, cert2 });
        }
    }

    [Fact]
    public void can_create_certificates_with_different_periods_but_same_gsrn()
    {
        var gsrn1 = GsrnHelper.GenerateRandom();
        var gsrn2 = GsrnHelper.GenerateRandom();
        var period = new Period(dateFrom: 42, dateTo: 420);

        var cert1 = new ConsumptionCertificate(
            gridArea: "dk1",
            period: period,
            meteringPointOwner: "owner1",
            gsrn: gsrn1,
            quantity: 42,
            blindingValue: new byte[] { 1, 2, 3 });

        var cert2 = new ConsumptionCertificate(
            gridArea: "dk1",
            period: period,
            meteringPointOwner: "owner1",
            gsrn: gsrn2,
            quantity: 42,
            blindingValue: new byte[] { 1, 2, 3 });

        using (var dbContext = new ApplicationDbContext(options))
        {
            dbContext.Update(cert1);
            dbContext.SaveChanges();
        }

        using (var dbContext = new ApplicationDbContext(options))
        {
            dbContext.Update(cert2);
            dbContext.SaveChanges();
        }

        using (var dbContext = new ApplicationDbContext(options))
        {
            var certificates = dbContext.ConsumptionCertificates.ToList();
            certificates.Should().BeEquivalentTo(new[] { cert1, cert2 });
        }
    }

    public void Dispose()
    {
        using var dbContext = new ApplicationDbContext(options);
        dbContext.RemoveAll(d => d.ConsumptionCertificates);

        GC.SuppressFinalize(this);
    }
}
