using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using API.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public class AcceptTermsTests
{
    private readonly IntegrationTestFixture _integrationTestFixture;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public AcceptTermsTests(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo).Options;
        _integrationTestFixture = integrationTestFixture;
    }

    [Fact]
    public async Task GivenValidRequest_WhenAcceptingTerms_ThenHttpOkAndTermsAccepted()
    {
        await using var context = new ApplicationDbContext(_options);

        var terms = Terms.Create(1);
        var orgCvr = Tin.Create("12345678");
        await SeedTerms(terms);

        var userApi = _integrationTestFixture.WebAppFactory.CreateApi(sub: Any.Guid().ToString(), orgCvr: orgCvr.Value);

        var response = await userApi.AcceptTerms();

        response.Should().Be200Ok();

        var result = await response.Content.ReadFromJsonAsync<AcceptTermsResponseDto>();
        result.Should().NotBeNull();
        result!.Message.Should().Be("Terms accepted successfully.");

        var organization = await context.Organizations.FirstOrDefaultAsync(o => o.Tin == orgCvr);
        organization.Should().NotBeNull();
        organization!.TermsAccepted.Should().BeTrue();
        organization.TermsVersion.Should().Be(terms.Version);
    }

    [Fact]
    public async Task GivenExistingOrganizationAndUser_WhenAcceptingTerms_ThenHttpOkAndTermsUpdated()
    {
        await using var context = new ApplicationDbContext(_options);

        var terms = Terms.Create(1);
        var orgCvr = Any.Tin();
        await SeedTerms(terms);

        var organization = Organization.Create(orgCvr, new OrganizationName("Existing Org"));
        var user = User.Create(IdpUserId.Create(Guid.NewGuid()), UserName.Create("Existing User"));
        await SeedOrganizationAndUser(organization, user);

        var userApi = _integrationTestFixture.WebAppFactory.CreateApi(sub: Any.Guid().ToString(), orgCvr: organization.Tin.Value);

        var response = await userApi.AcceptTerms();

        response.Should().Be200Ok();

        var result = await response.Content.ReadFromJsonAsync<AcceptTermsResponseDto>();
        result.Should().NotBeNull();
        result!.Message.Should().Be("Terms accepted successfully.");

        var updatedOrganization = await context.Organizations.FirstOrDefaultAsync(o => o.Tin == orgCvr);

        updatedOrganization.Should().NotBeNull();
        updatedOrganization!.TermsAccepted.Should().BeTrue();
        updatedOrganization.TermsVersion.Should().Be(terms.Version);
    }

    private async Task SeedTerms(Terms terms)
    {
        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Terms.AddAsync(terms);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedOrganizationAndUser(Organization organization, User user)
    {
        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Organizations.AddAsync(organization);
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
    }
}
