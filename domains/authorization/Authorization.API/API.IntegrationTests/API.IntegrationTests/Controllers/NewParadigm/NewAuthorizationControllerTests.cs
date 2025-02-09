using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.IntegrationTests.Setup.Fixtures;
using API.Models;
using API.UnitTests;
using API.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ClientType = API.Models.ClientType;

namespace API.IntegrationTests.Controllers.NewParadigm;

[Collection(nameof(BaseFixtureForTesting))]
public class NewAuthorizationControllerTests : DatabaseTest
{
    private readonly Api _api;
    private readonly TestWebApplicationFactory _integrationTestFixture;
    private readonly ApplicationDbContext _dbContext;

    public NewAuthorizationControllerTests(TestWebApplicationFactory integrationTestFixture) : base(integrationTestFixture)
    {
        _integrationTestFixture = integrationTestFixture;
        _api = integrationTestFixture.CreateApi(sub: _integrationTestFixture.IssuerIdpClientId.ToString());
        _dbContext = integrationTestFixture.Services.CreateScope().ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
    }

    [Fact]
    public async Task GivenExistingUserAndOrganization_WhenGettingConsent_ThenHttpOkAndCorrectResponseReturned()
    {
        var (idpUserId, tin, orgName) = await SeedData(_dbContext);

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
        result.OrgIds.Should().BeEmpty();
        result.Scope.Should().Be("dashboard production meters certificates wallet");
        result.TermsAccepted.Should().BeTrue();
    }

    [Fact]
    public async Task GivenNonExistingOrganization_WhenGettingConsent_ThenHttpOkAndTermsNotAccepted()
    {
        var client = Client.Create(new IdpClientId(_integrationTestFixture.IssuerIdpClientId), ClientName.Create("Internal Client"), ClientType.Internal, "https://localhost:5001");
        await _dbContext.Clients.AddAsync(client);
        await _dbContext.SaveChangesAsync();

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

    private async Task<(IdpUserId, Tin, OrganizationName)> SeedData(ApplicationDbContext dbContext)
    {
        if (!dbContext.Terms.Any())
        {
            dbContext.Terms.Add(Terms.Create(1));
            dbContext.SaveChanges();
        }

        var user = Any.User();
        var organization = Any.Organization(Any.Tin());
        organization.AcceptTerms(dbContext.Terms.First());

        await dbContext.Users.AddAsync(user);
        await dbContext.Organizations.AddAsync(organization);

        await dbContext.SaveChangesAsync();

        return (user.IdpUserId, organization.Tin!, organization.Name);
    }

    [Fact]
    public async Task GivenExistingUserAndOrganization_WhenGettingConsent_ThenAffiliationIsCreated()
    {

        var (idpUserId, tin, orgName) = await SeedData(_dbContext);

        var request = new AuthorizationUserRequest(
            Sub: idpUserId.Value,
            Name: "Test User",
            OrgCvr: tin.Value,
            OrgName: orgName.Value
        );

        var response = await _api.GetConsentForUser(request);

        response.Should().Be200Ok();

        _dbContext.Affiliations.Where(x => x.Organization.Tin == tin).ToList().Should().ContainSingle();
    }
}
