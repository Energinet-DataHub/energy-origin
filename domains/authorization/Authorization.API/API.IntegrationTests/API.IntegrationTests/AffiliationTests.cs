using API.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testing.Testcontainers;

namespace API.IntegrationTests;

public class AffiliationIntegrationTests : IClassFixture<PostgresContainer>
{
    private readonly ApplicationDbContext _context;

    public AffiliationIntegrationTests(PostgresContainer fixture)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CannotAddDuplicateAffiliation()
    {
        var user = new User();
        var organization = new Organization();
        var affiliation1 = Affiliation.Create(user, organization);
        var affiliation2 = Affiliation.Create(user, organization);

        _context.Users.Add(user);
        _context.Organizations.Add(organization);
        _context.Affiliations.Add(affiliation1);
        await _context.SaveChangesAsync();

        var act = async () =>
        {
            _context.Affiliations.Add(affiliation2);
            await _context.SaveChangesAsync();
        };

        var exception = await act.Should().ThrowAsync<DbUpdateException>();
        exception.WithMessage("*unique violation*");
        exception.And.InnerException.Should().BeOfType<PostgresException>();
    }
}
