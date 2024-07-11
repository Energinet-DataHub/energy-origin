using System;
using System.Linq;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testing.Helpers;
using Xunit;

namespace API.IntegrationTests.Repositories;

[Collection(IntegrationTestCollection.CollectionName)]
public class ConsumptionCertificateDatabaseTests
{
    private readonly DbContextOptions<ApplicationDbContext> options;

    public ConsumptionCertificateDatabaseTests(IntegrationTestFixture integrationTestFixture)
    {
        var dbTask = integrationTestFixture.PostgresContainer.CreateNewDatabase().Result;
        options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(dbTask.ConnectionString).Options;
        using var dbContext = new ApplicationDbContext(options);
        dbContext.Database.EnsureCreated();
        dbContext.Database.ExecuteSqlRaw("truncate table public.\"ConsumptionCertificates\"");
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
            dbContext.ConsumptionCertificates.Add(cert);
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
            dbContext.ConsumptionCertificates.Add(cert1);
            dbContext.SaveChanges();
        }

        using (var dbContext = new ApplicationDbContext(options))
        {
            dbContext.ConsumptionCertificates.Add(cert2);
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
            dbContext.ConsumptionCertificates.Add(cert1);
            dbContext.SaveChanges();
        }

        using (var dbContext = new ApplicationDbContext(options))
        {
            dbContext.ConsumptionCertificates.Add(cert2);
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
            dbContext.ConsumptionCertificates.Add(cert1);
            dbContext.SaveChanges();
        }

        using (var dbContext = new ApplicationDbContext(options))
        {
            dbContext.ConsumptionCertificates.Add(cert2);
            dbContext.SaveChanges();
        }

        using (var dbContext = new ApplicationDbContext(options))
        {
            var certificates = dbContext.ConsumptionCertificates.ToList();
            certificates.Should().BeEquivalentTo(new[] { cert1, cert2 });
        }
    }
}
