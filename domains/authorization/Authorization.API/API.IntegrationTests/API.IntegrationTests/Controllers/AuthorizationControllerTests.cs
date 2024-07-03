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
public class GetConsentForUserTests
{
    private readonly Api _api;
    private readonly IntegrationTestFixture _integrationTestFixture;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public GetConsentForUserTests(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo).Options;

        _integrationTestFixture = integrationTestFixture;
        _api = integrationTestFixture.WebAppFactory.CreateApi();
    }

    [Fact]
    public async Task GivenExistingUserAndOrganization_WhenGettingConsent_ThenHttpOkAndCorrectResponseReturned()
    {
        var (idpUserId, tin, orgName) = await SeedData();

        var request = new AuthorizationUserRequest(
            Sub: idpUserId.Value,
            Name: "Test User",
            OrgCvr: tin.Value,
            OrgName: orgName.Value
        );

        var userApi = _integrationTestFixture.WebAppFactory.CreateApi(sub: idpUserId.Value.ToString(), orgCvr: tin.Value);

        var response = await userApi.GetConsentForUser(request);

        response.Should().Be200Ok();

        var result = await response.Content.ReadFromJsonAsync<UserAuthorizationResponse>();
        result.Should().NotBeNull();
        result!.Sub.Should().Be(request.Sub);
        result.SubType.Should().Be("User");
        result.OrgName.Should().Be(request.OrgName);
        result.OrgIds.Should().NotBeEmpty();
        result.Scope.Should().Be("dashboard production meters certificates wallet");
        result.TermsAccepted.Should().BeTrue();
    }

    [Fact]
    public async Task GivenNonExistingOrganization_WhenGettingConsent_ThenHttpOkAndTermsNotAccepted()
    {
        var request = new AuthorizationUserRequest(
            Sub: Guid.NewGuid(),
            Name: "Test User",
            OrgCvr: "87654321",
            OrgName: "Non Existing Org"
        );

        var response = await _api.GetConsentForUser(request);

        response.Should().Be200Ok();

        var result = await response.Content.ReadFromJsonAsync<UserAuthorizationResponse>();
        result.Should().NotBeNull();
        result!.Sub.Should().Be(request.Sub);
        result.SubType.Should().Be("User");
        result.OrgName.Should().Be(request.OrgName);
        result.OrgIds.Should().BeEmpty();
        result.Scope.Should().Be("dashboard production meters certificates wallet");
        result.TermsAccepted.Should().BeFalse();
    }

    private async Task<(IdpUserId, Tin, OrganizationName)> SeedData()
    {
        var user = Any.User();
        var organization = Any.Organization(Tin.Create("12345678"));
        var terms = Terms.Create("1.0");
        organization.AcceptTerms(terms);
        var affiliation = Affiliation.Create(user, organization);

        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Users.AddAsync(user);
        await dbContext.Organizations.AddAsync(organization);
        await dbContext.Affiliations.AddAsync(affiliation);
        await dbContext.Terms.AddAsync(terms);

        await dbContext.SaveChangesAsync();
        return (user.IdpUserId, organization.Tin, organization.Name);
    }
}
