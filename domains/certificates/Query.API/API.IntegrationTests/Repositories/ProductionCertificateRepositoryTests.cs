using API.IntegrationTests.Testcontainers;
using CertificateValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace API.IntegrationTests.Repositories;

public class ProductionCertificateRepositoryTests : IClassFixture<PostgresContainer>
{
    private readonly DbContextOptions<ApplicationDbContext> options;

    public ProductionCertificateRepositoryTests(PostgresContainer dbContainer)
        => options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(dbContainer.ConnectionString).Options;

    [Fact(Skip = "for local dev tests")]
    public void Test()
    {
        {
            using var dbContext = new ApplicationDbContext(options);

            dbContext.Database.EnsureCreated();
        }

        var productionCertificate = new ProductionCertificate("dk1", new Period(42, 420),
            new Technology("fuel", "tech"), "owner1", "gsrn", 42, new byte[] { 1, 2, 3 });

        {
            using var dbContext = new ApplicationDbContext(options);
            dbContext.Update(productionCertificate);
            dbContext.SaveChanges();
        }

        var id = productionCertificate.Id;

        {
            using var dbContext = new ApplicationDbContext(options);
            var fetched = dbContext.ProductionCertificates.Find(id);

            fetched.Should().BeEquivalentTo(productionCertificate);

            fetched.Issue();

            dbContext.Update(fetched);
            dbContext.SaveChanges();
        }

        {
            using var dbContext = new ApplicationDbContext(options);
            var afterIssue = dbContext.ProductionCertificates.Find(id);
            //afterIssue.Should().BeEquivalentTo(fetched);
            afterIssue.IsIssued.Should().BeTrue();
        }
    }

    [Fact(Skip = "for local dev tests")]
    public async Task Test2()
    {
        {
            using var dbContext = new ApplicationDbContext(options);

            dbContext.Database.EnsureCreated();
        }

        var productionCertificate1 = new ProductionCertificate("dk1", new Period(42, 420),
            new Technology("fuel", "tech"), "owner1", "gsrn", 42, new byte[] { 1, 2, 3 });
        var productionCertificate2 = new ProductionCertificate("dk1", new Period(42, 420),
            new Technology("fuel", "tech"), "owner1", "gsrn", 42, new byte[] { 1, 2, 3 });

        {
            using var dbContext = new ApplicationDbContext(options);
            dbContext.Update(productionCertificate1);
            dbContext.SaveChanges();
        }

        {
            using var dbContext = new ApplicationDbContext(options);
            dbContext.Update(productionCertificate2);
            Action act = () => dbContext.SaveChanges();
            act.Should().Throw<DbUpdateException>();
        }

        {
            using var dbContext = new ApplicationDbContext(options);

            var cnt = await dbContext.ProductionCertificates.CountAsync();
            cnt.Should().Be(1);
        }
    }
}
