using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using API.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public class TermsControllerTests : IntegrationTestBase
{
    private readonly IntegrationTestFixture _integrationTestFixture;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public TermsControllerTests(IntegrationTestFixture integrationTestFixture) : base(integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo).Options;
        _integrationTestFixture = integrationTestFixture;
    }

    [Fact]
    public async Task GivenValidRequest_WhenAcceptingTerms_ThenHttpOkAndTermsAccepted()
    {
        await using var context = new ApplicationDbContext(_options);

        if (!context.Terms.Any())
        {
            context.Terms.Add(Terms.Create(1));
            context.SaveChanges();
        }

        var terms = context.Terms.First();
        var orgCvr = Tin.Create("12345678");

        var userApi = _integrationTestFixture.WebAppFactory.CreateApi(sub: Any.Guid().ToString(), orgCvr: orgCvr.Value, termsAccepted: false);

        var response = await userApi.AcceptTerms();

        response.Should().Be200Ok();

        var result = await response.Content.ReadFromJsonAsync<AcceptTermsResponse>(TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.Message.Should().Be("Terms accepted successfully.");

        var organization = await context.Organizations.FirstOrDefaultAsync(o => o.Tin == orgCvr, TestContext.Current.CancellationToken);
        organization.Should().NotBeNull();
        organization!.TermsAccepted.Should().BeTrue();
        organization.TermsVersion.Should().Be(terms.Version);
    }

    [Fact]
    public async Task GivenExistingOrganizationAndUser_WhenAcceptingTerms_ThenHttpOkAndTermsUpdated()
    {
        await using var context = new ApplicationDbContext(_options);

        if (!context.Terms.Any())
        {
            context.Terms.Add(Terms.Create(1));
            context.SaveChanges();
        }

        var terms = context.Terms.First();
        var orgCvr = Any.Tin();

        var organization = Organization.Create(orgCvr, OrganizationName.Create("Existing Org"));
        var user = User.Create(IdpUserId.Create(Guid.NewGuid()), UserName.Create("Existing User"));
        await SeedOrganizationAndUser(organization, user);

        var userApi = _integrationTestFixture.WebAppFactory.CreateApi(sub: Any.Guid().ToString(), orgCvr: organization.Tin!.Value,
            termsAccepted: false);

        var response = await userApi.AcceptTerms();

        response.Should().Be200Ok();

        var result = await response.Content.ReadFromJsonAsync<AcceptTermsResponse>(TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.Message.Should().Be("Terms accepted successfully.");

        var updatedOrganization = await context.Organizations.FirstOrDefaultAsync(o => o.Tin == orgCvr, TestContext.Current.CancellationToken);

        updatedOrganization.Should().NotBeNull();
        updatedOrganization!.TermsAccepted.Should().BeTrue();
        updatedOrganization.TermsVersion.Should().Be(terms.Version);
    }

    [Fact]
    public async Task GivenExistingOrganizationAndUser_WhenRevokingTerms_ThenHttpOkAndTermsUpdated()
    {
        // Given
        await using var context = new ApplicationDbContext(_options);

        if (!context.Terms.Any())
        {
            context.Terms.Add(Terms.Create(1));
            context.SaveChanges();
        }

        var terms = context.Terms.First();
        var orgCvr = Any.Tin();

        var organization = Organization.Create(orgCvr, OrganizationName.Create("Existing Org"));
        organization.AcceptTerms(terms, true);
        var user = User.Create(IdpUserId.Create(Guid.NewGuid()), UserName.Create("Existing User"));
        await SeedOrganizationAndUser(organization, user);

        var userApi = _integrationTestFixture.WebAppFactory.CreateApi(sub: Any.Guid().ToString(), orgCvr: organization.Tin!.Value, orgId: organization.Id.ToString(), termsAccepted: true);

        // When
        var response = await userApi.RevokeTerms();

        // Then
        var updatedOrganization = await context.Organizations.FirstOrDefaultAsync(o => o.Id == organization.Id, TestContext.Current.CancellationToken);

        response.Should().Be200Ok();
        var result = await response.Content.ReadFromJsonAsync<RevokeTermsResponse>(TestContext.Current.CancellationToken);
        result!.Message.Should().Be("Terms revoked successfully.");
        updatedOrganization!.TermsAccepted.Should().BeFalse();
        updatedOrganization.TermsVersion.Should().BeNull();
        updatedOrganization.TermsAcceptanceDate.Should().BeNull();
    }

    private async Task SeedOrganizationAndUser(Organization organization, User user)
    {
        var orgCvr = organization.Tin!.Value;
        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Whitelisted.AddAsync(Whitelisted.Create(Tin.Create(orgCvr)), CancellationToken.None);
        await dbContext.Organizations.AddAsync(organization);
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
    }
}
