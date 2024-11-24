using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using API.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;
using ClientType = API.Models.ClientType;

namespace API.IntegrationTests.Controllers;

public class AuthorizationControllerTests : IntegrationTestBase, IClassFixture<IntegrationTestFixture>, IAsyncLifetime
{
    private readonly Api _api;

    public AuthorizationControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
        _api = _fixture.WebAppFactory.CreateApi(sub: _fixture.WebAppFactory.IssuerIdpClientId.ToString());
    }

    [Fact]
    public async Task GivenExistingUserAndOrganization_WhenGettingConsent_ThenHttpOkAndCorrectResponseReturned()
    {
        var (idpUserId, tin, orgName) = await SeedData(_fixture.DbContext);

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
        var client = Client.Create(new IdpClientId(_fixture.WebAppFactory.IssuerIdpClientId), ClientName.Create("Internal Client"), ClientType.Internal, "https://localhost:5001");
        await _fixture.DbContext.Clients.AddAsync(client);
        await _fixture.DbContext.SaveChangesAsync();

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
        var (idpUserId, tin, orgName) = await SeedData(_fixture.DbContext);

        var request = new AuthorizationUserRequest(
            Sub: idpUserId.Value,
            Name: "Test User",
            OrgCvr: tin.Value,
            OrgName: orgName.Value
        );

        var response = await _api.GetConsentForUser(request);

        response.Should().Be200Ok();

        _fixture.DbContext.Affiliations.Where(x => x.Organization.Tin == tin).ToList().Should().ContainSingle();
    }
}
