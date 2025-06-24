using System.Net;
using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using API.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ClientType = API.Models.ClientType;

namespace API.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public class AuthorizationControllerTests : IntegrationTestBase
{
    private readonly Api _api;
    private readonly IntegrationTestFixture _integrationTestFixture;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public AuthorizationControllerTests(IntegrationTestFixture integrationTestFixture) : base(integrationTestFixture)
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
        var (idpUserId, tin, orgName) = await SeedData(dbContext);

        var request = new AuthorizationUserRequest(
            Sub: idpUserId.Value,
            Name: "Test User",
            OrgCvr: tin.Value,
            OrgName: orgName.Value
        );

        var response = await _api.GetConsentForUser(request);

        response.Should().Be200Ok();

        var result = await response.Content.ReadFromJsonAsync<UserAuthorizationResponse>(TestContext.Current.CancellationToken);
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
        await using var dbContext = new ApplicationDbContext(_options);
        var client = Client.Create(new IdpClientId(_integrationTestFixture.WebAppFactory.IssuerIdpClientId), ClientName.Create("Internal Client"), ClientType.Internal, "https://localhost:5001", false);
        await dbContext.Clients.AddAsync(client, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new AuthorizationUserRequest(
            Sub: Guid.NewGuid(),
            Name: "Test User",
            OrgCvr: "87654321",
            OrgName: "Non Existing Org"
        );

        var response = await _api.GetConsentForUser(request);

        response.Should().Be200Ok();

        var result = await response.Content.ReadFromJsonAsync<UserAuthorizationResponse>(TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.Sub.Should().Be(request.Sub);
        result.SubType.Should().Be("User");
        result.OrgName.Should().Be(request.OrgName);
        result.OrgIds.Should().BeEmpty();
        result.Scope.Should().Be("dashboard production meters certificates wallet");
        result.TermsAccepted.Should().BeFalse();
    }

    [Fact]
    public async Task GivenExistingUserAndOrganization_WhenGettingConsent_ThenAffiliationIsCreated()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        var (idpUserId, tin, orgName) = await SeedData(dbContext);

        var request = new AuthorizationUserRequest(
            Sub: idpUserId.Value,
            Name: "Test User",
            OrgCvr: tin.Value,
            OrgName: orgName.Value
        );

        var response = await _api.GetConsentForUser(request);

        response.Should().Be200Ok();

        dbContext.Affiliations.Where(x => x.Organization.Tin == tin).ToList().Should().ContainSingle();
    }

    [Fact]
    public async Task GivenNormalExistingTinThatIsWhitelisted_WhenCheckingWhitelist_ThenHttpOk()
    {
        var loginType = "normal";
        await using var dbContext = new ApplicationDbContext(_options);

        var organization = Any.Organization(Any.Tin());
        await dbContext.Organizations.AddAsync(organization, TestContext.Current.CancellationToken);
        await dbContext.Whitelisted.AddAsync(Whitelisted.Create(organization.Tin!), TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new WhitelistedOrganizationRequest(organization.Tin!.Value, loginType);

        var response = await _api.GetIsWhitelistedOrganization(request);

        response.Should().Be200Ok();
    }

    [Fact]
    public async Task GivenTrialExistingTinThatIsWhitelisted_WhenCheckingWhitelist_ThenHttpForbidden()
    {
        var loginType = "trial";
        await using var dbContext = new ApplicationDbContext(_options);

        var organization = Any.TrialOrganization(Any.Tin());
        await dbContext.Organizations.AddAsync(organization, TestContext.Current.CancellationToken);
        await dbContext.Whitelisted.AddAsync(Whitelisted.Create(organization.Tin!), TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new WhitelistedOrganizationRequest(organization.Tin!.Value, loginType);

        var response = await _api.GetIsWhitelistedOrganization(request);

        response.Should().Be403Forbidden();
    }

    [Fact]
    public async Task GivenTrialNonExistingTinThatIsNotWhitelisted_WhenCheckingWhitelist_ThenHttpOk()
    {
        var loginType = "trial";
        await using var dbContext = new ApplicationDbContext(_options);

        var organization = Any.TrialOrganization(Any.Tin());

        var request = new WhitelistedOrganizationRequest(organization.Tin!.Value, loginType);

        var response = await _api.GetIsWhitelistedOrganization(request);

        response.Should().Be200Ok();
    }

    [Fact]
    public async Task GivenNormalExistingTinThatIsNotWhitelisted_WhenCheckingWhitelist_ThenHttpForbidden()
    {
        var loginType = "normal";
        await using var dbContext = new ApplicationDbContext(_options);

        var organization = Any.Organization(Any.Tin());
        await dbContext.Organizations.AddAsync(organization, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new WhitelistedOrganizationRequest(organization.Tin!.Value, loginType);

        var response = await _api.GetIsWhitelistedOrganization(request);

        response.Should().HaveHttpStatusCode(HttpStatusCode.Forbidden);
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
        organization.AcceptTerms(dbContext.Terms.First(), true);

        await dbContext.Users.AddAsync(user);
        await dbContext.Organizations.AddAsync(organization);

        await dbContext.SaveChangesAsync();

        return (user.IdpUserId, organization.Tin!, organization.Name);
    }
}
