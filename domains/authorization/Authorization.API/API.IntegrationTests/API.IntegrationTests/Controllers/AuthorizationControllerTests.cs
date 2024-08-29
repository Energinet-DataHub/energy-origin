using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using API.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ClientType = API.Models.ClientType;

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
        _api = integrationTestFixture.WebAppFactory.CreateApi(sub: _integrationTestFixture.WebAppFactory.IssuerIdpClientId.ToString());
    }

    [Fact]
    public async Task GivenExistingUserAndOrganization_WhenGettingConsent_ThenHttpOkAndCorrectResponseReturned()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        var (idpUserId, tin, orgName) = await SeedData(dbContext, "12345678");

        var request = new AuthorizationUserRequest(
            Sub: idpUserId.Value,
            Name: "Test User",
            OrgCvr: tin.Value,
            OrgName: orgName.Value
        );

        var response = await _api.GetConsentForUser(request);

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
        await using var dbContext = new ApplicationDbContext(_options);
        var client = Client.Create(new IdpClientId(_integrationTestFixture.WebAppFactory.IssuerIdpClientId), ClientName.Create("Internal Client"), ClientType.Internal, "https://localhost:5001");
        await dbContext.Clients.AddAsync(client);
        await dbContext.SaveChangesAsync();

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

    private async Task<(IdpUserId, Tin, OrganizationName)> SeedData(ApplicationDbContext dbContext, string tin)
    {
        var user = Any.User();
        var organization = Any.Organization(Tin.Create(tin));
        organization.AcceptTerms(dbContext.Terms.First());

        await dbContext.Users.AddAsync(user);
        await dbContext.Organizations.AddAsync(organization);

        await dbContext.SaveChangesAsync();

        return (user.IdpUserId, organization.Tin, organization.Name);
    }

    [Fact]
    public async Task GivenExistingUserAndOrganization_WhenGettingConsent_ThenAffiliationIsCreated()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        var (idpUserId, tin, orgName) = await SeedData(dbContext, "12345679");

        var request = new AuthorizationUserRequest(
            Sub: idpUserId.Value,
            Name: "Test User",
            OrgCvr: tin.Value,
            OrgName: orgName.Value
        );

        var response = await _api.GetConsentForUser(request);

        response.Should().Be200Ok();

        dbContext.Affiliations.ToList().Should().ContainSingle();
    }
}
